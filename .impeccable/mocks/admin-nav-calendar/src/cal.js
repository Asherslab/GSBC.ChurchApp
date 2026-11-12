(function () {
  const CALS = [["Services", "Needs a publisher"], ["Impact Kids", ""], ["Impact Kids Team", ""], ["Life Groups", "Needs a publisher"]];
  window.calAside = function (opts) {
    const o = Object.assign({ chosen: "Services", grip: false, hover: null, lifted: null, dropAfter: null, arrange: false, arrangeButton: false, sliders: false, switches: true }, opts);
    const row = ([name, small], i) => {
      const chosen = name === o.chosen;
      const moves = o.arrange ? `<span class="moves"><button class="icon-btn icon-btn--small" aria-label="Move ${name} up" ${i === 0 ? "disabled" : ""}><i data-icon="arrow-up"></i></button><button class="icon-btn icon-btn--small" aria-label="Move ${name} down" ${i === CALS.length - 1 ? "disabled" : ""}><i data-icon="arrow-down"></i></button></span>` : "";
      const sw = !o.arrange && o.switches ? `<label class="switch switch--small"><input type="checkbox" role="switch" checked><span aria-hidden="true"></span></label>` : "";
      const sl = o.sliders ? `<button class="icon-btn icon-btn--small"><i data-icon="sliders"></i></button>` : "";
      const grip = o.grip || o.arrange ? `<span class="grip"><i data-icon="grip"></i></span>` : "";
      const li = `<li class="calendar-aside__calendar ${chosen && !o.arrange ? "is-chosen" : ""} ${o.hover === name ? "is-hover" : ""} ${o.lifted === name ? "is-lifted" : ""}">${grip}<button class="calendar-aside__name calendar-aside__name--pick" style="${grip ? "padding-left:4px" : ""}"><span>${name}${small ? `<small>${small}</small>` : ""}</span></button>${sl}${moves}${sw}</li>`;
      return li + (o.dropAfter === name ? `<li class="drop-line" aria-hidden="true"></li>` : "");
    };
    const head = o.arrange ? `<div class="arrange-head"><span>Order on phones and here</span><button class="btn btn--small btn--primary">Done</button></div>`
      : o.arrangeButton ? `<div class="arrange-head"><span>Calendars</span><button class="btn btn--small btn--quiet"><i data-icon="list"></i>Arrange</button></div>` : "";
    return `<div class="calendar-aside ${o.arrange ? "is-arranging" : ""}"><h1 class="calendar-aside__title">Calendar</h1>
      <nav class="calendar-aside__group"><button class="calendar-aside__item"><span>All your calendars</span></button><button class="calendar-aside__item"><span>Needs details</span></button><button class="calendar-aside__item"><span>Asked to publish</span><span class="count-capsule">2</span></button></nav>
      ${head}<ul class="calendar-aside__calendars">${CALS.map(row).join("")}</ul>${o.tip ? `<p class="tip">${o.tip}</p>` : ""}
      ${o.arrange ? "" : `<button class="calendar-aside__add"><i data-icon="plus"></i>New calendar</button>`}
      <nav class="calendar-aside__group calendar-aside__group--foot"><button class="calendar-aside__item"><span>Breaks</span><span class="calendar-aside__quiet-count">1</span></button></nav></div>`;
  };
  window.agenda = function () {
    const day = (h, d, rows) => `<section class="calendar-day"><h3 class="calendar-day__head">${h} <span>${d}</span></h3><div class="list">${rows.map(([t, n]) => `<div class="row"><span class="row__body" style="display:flex;gap:18px"><span class="quiet" style="min-width:64px;color:var(--ink-2)">${t}</span><span class="row__title">${n}</span></span></div>`).join("")}</div></section>`;
    return day("Sunday", "15 November", [["9am", "Life Groups"], ["10:30am", "Anniversary Sunday Service"], ["6pm", "Evening service"]]) + day("Sunday", "22 November", [["10:30am", "Morning service"], ["6pm", "Evening service"]]);
  };
})();
