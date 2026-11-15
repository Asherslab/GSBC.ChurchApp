import pathlib
schemes = {
  "light": dict(land="#f7f4ee", street="#ffffff", edge="#ddd5c7", park="#d9e6cf", walk="#00776d", ink="#18233a", ink2="#545e70", surface="#ffffff", marker="#e7ae74", page="#ffffff", line="rgba(42,61,94,.12)", label="#545e70", primary="#2a3d5e"),
  "dark": dict(land="#141d31", street="#26324b", edge="#1f293f", park="#1c3528", walk="#2ec4b6", ink="#f2ede4", ink2="#b2b8c5", surface="#1d2840", marker="#e7ae74", page="#141d31", line="rgba(255,255,255,.09)", label="#b2b8c5", primary="#2ec4b6"),
}
streets = [((20,60),(380,60)), ((20,150),(380,150)), ((20,240),(380,240)), ((80,20),(80,290)), ((200,20),(200,290)), ((320,20),(320,290))]
chosen = ((200,150),(320,150))
def mix(a,b,t):
    a=[int(a[i:i+2],16) for i in (1,3,5)]; b=[int(b[i:i+2],16) for i in (1,3,5)]
    return "#%02x%02x%02x"%tuple(round(x+(y-x)*t) for x,y in zip(a,b))
covered = [(((20,60),(200,60)),0.05),(((200,60),(380,60)),0.4),(((80,150),(200,150)),0.1),((chosen),0.15),(((200,20),(200,150)),0.6),(((320,150),(320,290)),0.0),(((20,240),(200,240)),0.8)]
def line(p,c,w,extra=""):
    return f'<line x1="{p[0][0]}" y1="{p[0][1]}" x2="{p[1][0]}" y2="{p[1][1]}" stroke="{c}" stroke-width="{w}" stroke-linecap="round" {extra}/>'
def sides(p,c,w):
    (x1,y1),(x2,y2)=p; o=3.2
    if y1==y2: return line(((x1,y1-o),(x2,y2-o)),c,w)+line(((x1,y1+o),(x2,y2+o)),c,w)
    return line(((x1-o,y1),(x2-o,y2)),c,w)+line(((x1+o,y1),(x2+o,y2)),c,w)
def svg(s, look):
    k=schemes[s]; out=[f'<svg viewBox="0 0 400 310" role="img" aria-label="Coverage map, {s}"><rect width="400" height="310" fill="{k["land"]}"/>',
      f'<rect x="215" y="170" width="90" height="55" rx="6" fill="{k["park"]}"/>']
    for p in streets: out.append(line(p,k["edge"],14))
    for p in streets: out.append(line(p,k["street"],11))
    if look=="casing": out.append(line(chosen,k["ink"],19)); out.append(line(chosen,k["surface"],13))
    for p,age in covered: out.append(sides(p,mix(k["walk"],k["land"],age),3.2))
    if look=="marker":
        out.append(line(chosen,k["surface"],12)); out.append(line(chosen,k["marker"],8))
    if look=="dashed":
        out.append(line(chosen,k["ink"],3,'stroke-dasharray="7 5"'))
    if look=="current":
        out.append(line(chosen,k["surface"],12)); out.append(line(chosen,k["primary"],8))
    out.append(f'<text x="262" y="140" font-size="10" fill="{k["label"]}" text-anchor="middle" font-family="Montserrat, sans-serif" paint-order="stroke" stroke="{k["land"]}" stroke-width="3">Sample Street</text>')
    out.append('</svg>'); return "".join(out)
def page(look, title, note):
    panes="".join(f'''<figure class="pane {s}"><div class="map">{svg(s,look)}</div><figcaption><span class="t">Sample Street</span><span class="c">Both sides · 46 letterboxes · done 3 days ago</span></figcaption></figure>''' for s in ("light","dark"))
    return f'''<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>{title}</title><style>
body{{margin:0;font-family:Montserrat,-apple-system,system-ui,sans-serif;background:#f4efe7;color:#18233a;padding:20px}}
h1{{font-size:18px;margin:0 0 4px}} p.n{{margin:0 0 16px;font-size:14px;color:#545e70;max-width:62ch}}
.row{{display:grid;grid-template-columns:1fr 1fr;gap:16px}} @media(max-width:700px){{.row{{grid-template-columns:1fr}}}}
.pane{{margin:0;border-radius:14px;overflow:hidden;box-shadow:0 1px 3px rgba(0,0,0,.12)}} .pane svg{{display:block;width:100%;height:auto}}
.pane figcaption{{display:grid;gap:2px;padding:10px 14px 12px;font-size:13px}} .pane .t{{font-weight:600}}
.light figcaption{{background:#fff;color:#18233a}} .light .c{{color:#545e70}} .dark figcaption{{background:#1d2840;color:#f2ede4}} .dark .c{{color:#b2b8c5}}
</style></head><body><h1>{title}</h1><p class="n">{note}</p><div class="row">{panes}</div></body></html>'''
looks = {
 "current": ("Today: navy (bright aqua at night)", "What it does now: in light the chosen street is navy beside deep-aqua coverage; at night it is the same bright aqua as streets done this week."),
 "casing": ("A: An ink outline round it", "The chosen street is outlined in ink (light ink at night) with its coverage still drawn inside, so you still see which sides were done and how long ago while it stands out from every other street."),
 "marker": ("B: The orange marker", "The chosen street turns orange with a page-coloured edge, as the chosen tile does on the Hub canvas. Loudest, but it hides the street's own coverage."),
 "dashed": ("C: A dashed ink line", "A dashed ink line runs along the chosen street, like the dashed loop of Draw round streets. Coverage shows either side, but it's thin when zoomed out."),
}
for k,(t,n) in looks.items(): pathlib.Path(f"selection-{k}.html").write_text(page(k,t,n))
