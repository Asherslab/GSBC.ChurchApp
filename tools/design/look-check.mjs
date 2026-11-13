import { chromium } from 'playwright-core';
import { execFileSync } from 'node:child_process';
import { createHash, randomBytes } from 'node:crypto';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';

const here = dirname(fileURLToPath(import.meta.url));
const repo = execFileSync('git', ['-C', here, 'rev-parse', '--show-toplevel'], { encoding: 'utf8' }).trim();
const analyse = readFileSync(join(here, 'look-check.page.js'), 'utf8');

const { values: args } = parseArgs({
  options: {
    base: { type: 'string', default: 'http://localhost:5400' },
    out: { type: 'string' },
    widths: { type: 'string', default: '1440,1024,390' },
    schemes: { type: 'string', default: 'light,dark' },
    only: { type: 'string' },
    extra: { type: 'string', default: '' },
    jobs: { type: 'string', default: '6' },
    gutter: { type: 'string', default: '8' },
    skip: { type: 'string', default: '.phone-frame, .lock-proof' },
  },
});

const base = args.base.replace(/\/$/, '');
const out = args.out ?? join(process.env.TMPDIR ?? '/tmp', 'gsbc-look-check', new Date().toISOString().replace(/[:.]/g, '-'));
mkdirSync(join(out, 'shots'), { recursive: true });
const widths = args.widths.split(',').map(Number);
const schemes = args.schemes.split(',');
const gutter = Number(args.gutter);
const only = args.only ? new RegExp(args.only) : null;
const settleLimit = 8000;

const statePath = join(process.env.XDG_CACHE_HOME ?? join(homedir(), '.cache'), 'gsbc-look-check', createHash('sha256').update(`${repo}|${base}`).digest('hex').slice(0, 12) + '.json');

const settle = page => page.evaluate(async limit => {
  const started = performance.now();
  let changed = performance.now();
  const observer = new MutationObserver(() => { changed = performance.now(); });
  observer.observe(document.body, { subtree: true, childList: true, attributes: true, characterData: true });
  try {
    while (performance.now() - started < limit) {
      await new Promise(resolve => setTimeout(resolve, 100));
      const loading = document.querySelector('.skeleton, [aria-busy="true"], #app > .loading, .loading-progress');
      if (!loading && performance.now() - changed > 400) break;
    }
    await document.fonts.ready;
    await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
  } finally {
    observer.disconnect();
  }
}, settleLimit);

const waitForApp = async page => {
  await page.waitForFunction(() => document.querySelector('.admin-shell') || document.querySelector('.sign-in-stage, .signin'), null, { timeout: 60000 });
  await settle(page);
};

const go = async (page, path) => {
  const target = '/' + path.replace(/^\//, '');
  const ready = await page.evaluate(() => typeof window.Blazor?.navigateTo === 'function' && !!document.querySelector('.admin-shell')).catch(() => false);
  if (ready) {
    await page.evaluate(target => window.Blazor.navigateTo(target), target);
    await page.waitForFunction(target => location.pathname + location.search === target || location.pathname === target, target, { timeout: 10000 }).catch(() => {});
  } else {
    await page.goto(base + target);
    await waitForApp(page);
  }
  await page.evaluate(() => {
    window.scrollTo(0, 0);
    document.querySelectorAll('*').forEach(element => { if (element.scrollTop) element.scrollTop = 0; });
  });
  await settle(page);
};

const authenticator = async (context, page) => {
  const cdp = await context.newCDPSession(page);
  await cdp.send('WebAuthn.enable');
  const { authenticatorId } = await cdp.send('WebAuthn.addVirtualAuthenticator', {
    options: { protocol: 'ctap2', transport: 'internal', hasResidentKey: true, hasUserVerification: true, isUserVerified: true, automaticPresenceSimulation: true },
  });
  return { cdp, authenticatorId };
};

const signedIn = page => page.getByText(/Signed in as/).first().isVisible().catch(() => false);

const sessionPath = statePath.replace(/\.json$/, '.session.json');

const signIn = async browser => {
  if (existsSync(sessionPath)) {
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, storageState: sessionPath });
    const page = await context.newPage();
    await page.goto(base + '/account');
    await waitForApp(page);
    if (await signedIn(page)) return { context, page };
    await context.close();
  }
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();
  const { cdp, authenticatorId } = await authenticator(context, page);
  const saved = existsSync(statePath) ? JSON.parse(readFileSync(statePath, 'utf8')) : null;
  await page.goto(base + '/account');
  await waitForApp(page);

  if (saved) {
    for (const credential of saved.credentials) await cdp.send('WebAuthn.addCredential', { authenticatorId, credential });
    await page.getByRole('button', { name: 'Sign in with a passkey' }).click();
    await page.getByText(/Signed in as/).first().waitFor({ timeout: 15000 }).catch(() => {});
    if (await signedIn(page)) {
      console.log(`Signed in as ${saved.email}.`);
      return { context, page };
    }
    console.log(`${saved.email} couldn't sign in here (a fresh database?); making a new test account.`);
    await page.goto(base + '/account');
    await waitForApp(page);
  }

  for (const email of ['e2e+look@example.com', `e2e+look-${randomBytes(3).toString('hex')}@example.com`]) {
    await page.getByText('Create an account').click();
    await page.getByLabel('Your name').fill('Look Check');
    await page.getByLabel('Email').fill(email);
    await page.getByText("I'm 18 or older").click();
    await page.getByRole('button', { name: 'Create a passkey' }).click();
    await page.getByText(/Signed in as/).first().waitFor({ timeout: 15000 }).catch(() => {});
    if (!(await signedIn(page))) {
      await page.goto(base + '/account');
      await waitForApp(page);
      continue;
    }
    const { credentials } = await cdp.send('WebAuthn.getCredentials', { authenticatorId });
    mkdirSync(dirname(statePath), { recursive: true });
    writeFileSync(statePath, JSON.stringify({ email, credentials }), { mode: 0o600 });
    console.log(execFileSync(join(repo, 'tools/access/make-owner.sh'), [email, '--lead-every-ministry'], { encoding: 'utf8' }).trim());
    await page.reload();
    await waitForApp(page);
    return { context, page };
  }
  throw new Error(`Couldn't sign up a test account on ${base}/account. Is this worktree's AppHost running with Accounts__Passkeys__RelyingPartyId=localhost?`);
};

const recordSelector = '.workbench__main :is(a.row, button.row, tbody tr, .board-card, a.card, button.card)';

const discover = async page => {
  await go(page, '/');
  for (const head of await page.$$('.admin-sidebar button.module-nav__area-head[aria-expanded=false]')) await head.click();
  await settle(page);
  const sidebar = await page.$$eval('.admin-sidebar a[href]', links => links.map(link => new URL(link.href).pathname.replace(/^\//, '')));
  const extra = args.extra.split(',').map(path => path.trim().replace(/^\//, '')).filter(Boolean);
  const queue = [...new Set([...sidebar, 'account', ...extra])].map(path => ({ path, depth: 0 }));
  const seen = new Set(queue.map(item => item.path));
  const shots = [];
  while (queue.length) {
    const { path, depth } = queue.shift();
    await go(page, path);
    const current = (await page.evaluate(() => location.pathname)).replace(/^\//, '');
    if (current !== path && seen.has(current)) continue;
    shots.push({ path, steps: [] });
    const found = await page.evaluate(() => ({
      links: [...document.querySelectorAll('.admin-main .tab-strip a.tab-strip__tab[href]')].map(link => new URL(link.href).pathname.replace(/^\//, '')),
      tabs: [...document.querySelectorAll('.admin-main [role=tab]')].map((tab, index) => ({ tab: index, label: tab.innerText.trim().split('\n')[0], selected: tab.getAttribute('aria-selected') === 'true' })).filter(tab => !tab.selected),
      workbench: !!document.querySelector('.workbench__main'),
      open: !!document.querySelector('.workbench__detail'),
    }));
    for (const link of found.links) {
      if (seen.has(link) || depth >= 3) continue;
      seen.add(link);
      queue.push({ path: link, depth: depth + 1 });
    }
    for (const { tab, label } of found.tabs) shots.push({ path, steps: [{ tab, label }] });
    if (found.workbench && !found.open && depth < 3) {
      const record = page.locator(recordSelector).first();
      if (await record.count()) {
        await record.click().catch(() => {});
        await settle(page);
        const opened = (await page.evaluate(() => location.pathname + location.search)).replace(/^\//, '');
        if (opened !== path && !seen.has(opened)) {
          seen.add(opened);
          queue.push({ path: opened, depth: depth + 1 });
        } else if (opened === path && await page.locator('.workbench__detail').count()) {
          shots.push({ path, steps: [{ record: true }] });
        }
      }
    }
  }
  return shots.filter(shot => !only || only.test(shot.path));
};

const nameOf = shot => (shot.path || 'home').replace(/[^\w-]+/g, '_') + shot.steps.map(step => step.label ? '__' + step.label.replace(/[^\w-]+/g, '_') : '__record').join('');

const annotate = (page, findings) => page.evaluate(findings => {
  const layer = document.createElement('div');
  layer.id = 'look-check-marks';
  layer.style.cssText = 'position:absolute;inset:0;pointer-events:none;z-index:2147483647';
  findings.forEach((finding, index) => {
    const mark = document.createElement('div');
    mark.style.cssText = `position:absolute;left:${finding.rect.x - 2}px;top:${finding.rect.y - 2}px;width:${finding.rect.w + 4}px;height:${finding.rect.h + 4}px;outline:2px solid #ff2d55;background:rgb(255 45 85 / .12)`;
    const label = document.createElement('span');
    label.textContent = String(index + 1);
    label.style.cssText = 'position:absolute;left:-2px;top:-18px;font:700 11px/16px system-ui;padding:0 4px;background:#ff2d55;color:#fff;border-radius:3px';
    mark.append(label);
    layer.append(mark);
  });
  document.body.append(layer);
}, findings);

const run = async (browser, storageState, variant, shots, results) => {
  const height = variant.width <= 480 ? 844 : 900;
  const context = await browser.newContext({ storageState, viewport: { width: variant.width, height }, colorScheme: variant.scheme, hasTouch: variant.width <= 480 });
  const page = await context.newPage();
  page.setDefaultTimeout(8000);
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.goto(base + '/' + (shots[0]?.path ?? ''));
  await waitForApp(page);
  for (const shot of shots) {
    const name = `${nameOf(shot)}--${variant.scheme}-${variant.width}`;
    const began = Date.now();
    try {
      if (shot.steps.length) {
        await page.goto(base + '/' + shot.path);
        await waitForApp(page);
      } else {
        await go(page, shot.path);
      }
      for (const step of shot.steps) {
        if (step.tab !== undefined) await page.locator('.admin-main [role=tab]').nth(step.tab).click();
        if (step.record) await page.locator(recordSelector).first().click();
        await settle(page);
      }
      const result = await page.evaluate(`(${analyse})(${JSON.stringify({ gutter, skip: args.skip })})`);
      await page.screenshot({ path: join(out, 'shots', name + '.png') });
      if (result.findings.length) {
        await annotate(page, result.findings);
        await page.screenshot({ path: join(out, 'shots', name + '.marked.png'), fullPage: true });
        await page.evaluate(() => document.getElementById('look-check-marks')?.remove());
      }
      if (result.scheme && result.scheme !== variant.scheme) result.findings.push({ check: 'broken', what: 'the page', text: `asked for ${variant.scheme} but showed ${result.scheme}`, against: '', rect: { x: 0, y: 0, w: 10, h: 10 } });
      results.push({ shot, variant, name, findings: result.findings, marked: result.findings.length > 0, ms: Date.now() - began });
    } catch (error) {
      results.push({ shot, variant, name, findings: [{ check: 'broken', what: 'the look check', text: error.message.split('\n')[0], against: '', rect: { x: 0, y: 0, w: 10, h: 10 } }], marked: false });
    }
  }
  if (errors.length) results.push({ shot: { path: '(scripts)', steps: [] }, variant, name: `errors--${variant.scheme}-${variant.width}`, findings: [...new Set(errors)].map(text => ({ check: 'broken', what: 'a script error', text, against: '', rect: { x: 0, y: 0, w: 10, h: 10 } })), marked: false });
  await context.close();
};

const escape = text => String(text ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);
const checks = {
  edge: 'Too close to an edge',
  spills: 'Spills out of its box',
  clipped: 'Text cut off',
  overlap: 'Overlapping',
  overflow: 'Wider than its space',
  bar: 'Bar not flush',
  broken: 'Page broken',
};

const signature = finding => `${finding.check}|${finding.side ?? ''}|${finding.what}|${finding.against}`;

const report = (results, started) => {
  const groups = new Map();
  for (const result of results) {
    for (const finding of result.findings) {
      const key = signature(finding);
      const group = groups.get(key) ?? { finding, where: new Set(), count: 0 };
      group.where.add(`${result.shot.path || 'home'} (${result.variant.scheme} ${result.variant.width})`);
      group.count++;
      groups.set(key, group);
    }
  }
  const ranked = [...groups.values()].sort((a, b) => b.where.size - a.where.size);
  const byShot = new Map();
  for (const result of results) {
    const key = nameOf(result.shot);
    byShot.set(key, [...(byShot.get(key) ?? []), result]);
  }
  const total = results.reduce((sum, result) => sum + result.findings.length, 0);
  const html = `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Look check</title>
<style>
:root { color-scheme: light dark; --bg: #f7f5f1; --ink: #18233a; --ink-2: #545e70; --line: rgb(42 61 94 / .14); --card: #fff; --flag: #c62848; }
@media (prefers-color-scheme: dark) { :root { --bg: #141d31; --ink: #f2ede4; --ink-2: #b2b8c5; --line: rgb(255 255 255 / .1); --card: #1d2840; --flag: #ff6b86; } }
body { margin: 0; background: var(--bg); color: var(--ink); font: 15px/1.45 -apple-system, system-ui, sans-serif; }
main { max-width: 1400px; margin: 0 auto; padding: 24px 16px 64px; }
h1 { margin: 0 0 4px; font-size: 28px; } h2 { margin: 40px 0 8px; font-size: 20px; } h3 { margin: 0 0 8px; font-size: 16px; }
.lead { color: var(--ink-2); margin: 0 0 24px; }
table { width: 100%; border-collapse: collapse; background: var(--card); border-radius: 10px; overflow: hidden; }
td, th { padding: 8px 12px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; font-size: 13px; }
th { font-size: 12px; color: var(--ink-2); }
code { font-size: 12px; overflow-wrap: anywhere; }
.page { background: var(--card); border-radius: 12px; padding: 16px; margin: 16px 0; }
.variants { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 12px; }
.variant img { width: 100%; max-height: 280px; object-fit: cover; object-position: top; border: 1px solid var(--line); border-radius: 6px; background: #fff; }
.variant figcaption { font-size: 12px; color: var(--ink-2); margin-top: 4px; }
.flagged figcaption { color: var(--flag); font-weight: 600; }
ol { margin: 8px 0 0; padding-left: 20px; font-size: 13px; } li { margin: 2px 0; }
.check { font-weight: 600; }
</style></head><body><main>
<h1>Look check</h1>
<p class="lead">${escape(base)} · ${results.length} screens (${byShot.size} pages × ${schemes.join(' and ')} × ${widths.join(', ')}px) · ${total} findings in ${ranked.length} distinct places · ${Math.round((Date.now() - started) / 1000)}s</p>
<h2>What to fix, most widespread first</h2>
<table><thead><tr><th>Check</th><th>What</th><th>Against</th><th>Seen on</th></tr></thead><tbody>
${ranked.map(group => `<tr><td class="check">${escape(checks[group.finding.check])}${group.finding.side ? ' (' + group.finding.side + ', ' + group.finding.distance + 'px)' : ''}</td><td><code>${escape(group.finding.what)}</code><br>${escape(group.finding.text)}</td><td><code>${escape(group.finding.against)}</code></td><td>${group.where.size} screens<br><small>${escape([...group.where].slice(0, 4).join(', '))}${group.where.size > 4 ? '…' : ''}</small></td></tr>`).join('\n')}
</tbody></table>
<h2>Every page</h2>
${[...byShot.entries()].map(([key, list]) => `<section class="page"><h3>/${escape(list[0].shot.path)}${list[0].shot.steps.map(step => step.label ? ' · tab ' + escape(step.label) : ' · a record open').join('')}</h3>
<div class="variants">${list.map(result => `<figure class="variant ${result.findings.length ? 'flagged' : ''}"><a href="shots/${escape(result.name)}${result.marked ? '.marked' : ''}.png"><img loading="lazy" src="shots/${escape(result.name)}${result.marked ? '.marked' : ''}.png" alt=""></a><figcaption>${result.variant.scheme} ${result.variant.width}px · ${result.findings.length ? result.findings.length + ' found' : 'nothing found'} · <a href="shots/${escape(result.name)}.png">clean</a></figcaption>
${result.findings.length ? `<ol>${result.findings.map(finding => `<li><span class="check">${escape(checks[finding.check])}</span>${finding.side ? ` (${finding.side}, ${finding.distance}px)` : ''}: <code>${escape(finding.what)}</code> ${escape(finding.text)}${finding.against ? ` · <code>${escape(finding.against)}</code>` : ''}</li>`).join('')}</ol>` : ''}</figure>`).join('')}</div></section>`).join('\n')}
</main></body></html>`;
  writeFileSync(join(out, 'index.html'), html);
  writeFileSync(join(out, 'findings.json'), JSON.stringify(results.map(({ shot, variant, name, findings, ms }) => ({ path: shot.path, steps: shot.steps, variant, name, ms, findings })), null, 2));
  return { ranked, total };
};

const started = Date.now();
const browser = await chromium.launch();
try {
  const { context, page } = await signIn(browser);
  const shots = await discover(page);
  const storageState = await context.storageState();
  writeFileSync(sessionPath, JSON.stringify(storageState), { mode: 0o600 });
  await context.close();
  console.log(`Checking ${shots.length} pages at ${widths.join(', ')}px in ${schemes.join(' and ')}…`);

  const variants = schemes.flatMap(scheme => widths.map(width => ({ scheme, width })));
  const jobs = Math.max(1, Number(args.jobs));
  const work = variants.flatMap(variant => {
    const per = Math.max(1, Math.ceil(variants.length / jobs));
    const slices = Math.max(1, Math.floor(jobs / variants.length));
    return Array.from({ length: slices }, (_, slice) => ({ variant, shots: shots.filter((_, index) => index % slices === slice), per }));
  });
  const results = [];
  const queue = [...work];
  await Promise.all(Array.from({ length: jobs }, async () => {
    for (let item = queue.shift(); item; item = queue.shift()) await run(browser, storageState, item.variant, item.shots, results);
  }));

  const order = new Map(shots.map((shot, index) => [nameOf(shot), index]));
  results.sort((a, b) => (order.get(nameOf(a.shot)) ?? 1e9) - (order.get(nameOf(b.shot)) ?? 1e9) || a.variant.scheme.localeCompare(b.variant.scheme) || b.variant.width - a.variant.width);
  const { ranked, total } = report(results, started);
  for (const group of ranked.slice(0, 25)) console.log(`  ${checks[group.finding.check]}${group.finding.side ? ` (${group.finding.side}, ${group.finding.distance}px)` : ''}: ${group.finding.what} — ${group.finding.text}  [${group.where.size} screens]`);
  if (ranked.length > 25) console.log(`  … and ${ranked.length - 25} more`);
  console.log(`${total} findings in ${ranked.length} places across ${results.length} screens, ${Math.round((Date.now() - started) / 1000)}s.`);
  console.log(`Report: ${join(out, 'index.html')}`);
  process.exitCode = total ? 1 : 0;
} finally {
  await browser.close();
}
