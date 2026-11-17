import pathlib
S = {
 "light": dict(land="#f7f4ee", street="#ffffff", edge="#ddd5c7", park="#d9e6cf", walk="#00776d", walkedge="#ffffff", you="#e7ae74", ring="#ffffff", ink="#18233a", ink2="#545e70", page="#ffffff", navy="#2a3d5e", beige="#d5c9b7", sand="#f4efe7", line="rgba(42,61,94,.12)"),
 "dark": dict(land="#141d31", street="#26324b", edge="#1f293f", park="#1c3528", walk="#2ec4b6", walkedge="#141d31", you="#e7ae74", ring="#1d2840", ink="#f2ede4", ink2="#b2b8c5", page="#141d31", navy="#283c62", beige="#d5c9b7", sand="#26334f", line="rgba(255,255,255,.09)"),
}
streets=[((-20,140),(420,-120)),((-20,330),(420,70)),((120,-20),(250,330)),((40,300),(160,520)),((300,200),(420,420))]
route=[((-20,330),(150,230)),((150,230),(250,170)),((250,170),(200,90)),((200,90),(420,-40))]
def ln(p,c,w,cap="round",extra=""): return f'<line x1="{p[0][0]}" y1="{p[0][1]}" x2="{p[1][0]}" y2="{p[1][1]}" stroke="{c}" stroke-width="{w}" stroke-linecap="{cap}" {extra}/>'
import math
def fan(cx,cy,heading,spread,radius,colour,opacity,gid):
    a1=math.radians(heading-spread/2-90); a2=math.radians(heading+spread/2-90)
    x1,y1=cx+radius*math.cos(a1),cy+radius*math.sin(a1); x2,y2=cx+radius*math.cos(a2),cy+radius*math.sin(a2)
    large=1 if spread>180 else 0
    return (f'<defs><radialGradient id="{gid}" cx="{cx}" cy="{cy}" r="{radius}" gradientUnits="userSpaceOnUse"><stop offset="0" stop-color="{colour}" stop-opacity="{opacity}"/><stop offset="1" stop-color="{colour}" stop-opacity="0"/></radialGradient></defs>'
            f'<path d="M{cx},{cy} L{x1:.1f},{y1:.1f} A{radius},{radius} 0 {large} 1 {x2:.1f},{y2:.1f} Z" fill="url(#{gid})"/>')
def pointer(cx,cy,heading,colour,ring):
    return f'<g transform="rotate({heading} {cx} {cy})"><path d="M{cx},{cy-24} L{cx+8},{cy-11} L{cx-8},{cy-11} Z" fill="{colour}" stroke="{ring}" stroke-width="2.5" stroke-linejoin="round"/></g>'
def mapsvg(s,look,sure,label):
    k=S[s]; cx,cy=205,190; h=35
    o=[f'<svg viewBox="0 0 400 380" role="img" aria-label="{label}"><rect width="400" height="380" fill="{k["land"]}"/><rect x="270" y="250" width="120" height="110" rx="10" fill="{k["park"]}"/>']
    o+= [ln(p,k["edge"],22) for p in streets]+[ln(p,k["street"],18) for p in streets]
    o+= [ln(p,k["walkedge"],11) for p in route]+[ln(p,k["walk"],7) for p in route]
    o.append(f'<text x="330" y="40" font-size="11" fill="{k["ink2"]}" font-family="Montserrat,sans-serif" paint-order="stroke" stroke="{k["land"]}" stroke-width="3" transform="rotate(-31 330 40)">Greensill Rd</text>')
    spread = 50 if sure else 110
    if look=="fan":
        o.append(fan(cx,cy,h,spread,110 if sure else 90,k["you"],.9 if sure else .5,f"g{s}{sure}"))
    if look=="beam":
        o.append(fan(cx,cy,h,spread,70 if sure else 58,k["you"],1 if sure else .6,f"b{s}{sure}"))
    if look=="ink":
        o.append(fan(cx,cy,h,spread,100 if sure else 84,k["ink"],.5 if sure else .28,f"i{s}{sure}"))
    o.append(f'<circle cx="{cx}" cy="{cy}" r="28" fill="{k["you"]}" fill-opacity=".16"/>')
    if look=="pointer" and sure: o.append(pointer(cx,cy,h,k["you"],k["ring"]))
    o.append(f'<circle cx="{cx}" cy="{cy}" r="11" fill="{k["you"]}" stroke="{k["ring"]}" stroke-width="4"/>')
    o.append('</svg>'); return "".join(o)
def phone(s,look,sure):
    k=S[s]
    return f'''<figure class="phone {s}"><div class="mast" style="background:{k["navy"]}"><p class="st" style="color:{k["beige"]}"><span>Walking · 4 min</span><span>Carols sample</span></p><p class="street">Greensill Rd</p><p class="side">Houses on your left · 13 to go</p></div>
<div class="map">{mapsvg(s,look,sure,"Your walk with you on it, "+s)}</div><figcaption style="background:{k["page"]};color:{k["ink2"]}">{"The compass is sure" if sure else "The compass isn't sure (indoors, near metal)"}</figcaption></figure>'''
def page(look,title,note):
    cells="".join(phone(s,look,sure) for s in ("light","dark") for sure in (True,False))
    return f'''<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>{title}</title><style>
body{{margin:0;font-family:Montserrat,-apple-system,system-ui,sans-serif;background:#f4efe7;color:#18233a;padding:20px}}
h1{{font-size:18px;margin:0 0 4px}} p.n{{margin:0 0 16px;font-size:14px;color:#545e70;max-width:64ch;line-height:1.45}}
.row{{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:14px}} @media(max-width:1000px){{.row{{grid-template-columns:repeat(2,minmax(0,1fr))}}}}
.phone{{margin:0;border-radius:22px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,.18);background:#000}}
.mast{{padding:14px 16px 12px}} .mast p{{margin:0}} .st{{display:flex;justify-content:space-between;font-size:11px;font-weight:600}}
.street{{color:#fff;font-weight:700;font-size:26px;letter-spacing:-.03em;margin-top:4px!important}} .side{{color:#fff;font-weight:600;font-size:13px}}
.map svg{{display:block;width:100%;height:auto}} figcaption{{padding:9px 14px;font-size:12px}}
</style></head><body><h1>{title}</h1><p class="n">{note}</p><div class="row">{cells}</div></body></html>'''
looks={
 "fan":("A: A soft fan, like Apple Maps","A wide orange fan from the dot, fading out as it goes, pointing where the phone faces. When the compass isn't sure it widens and fades; with no idea at all it goes and only the dot stays."),
 "beam":("B: A short, stronger beam","The same fan, shorter and more solid, so it reads in bright sun but covers less of the street names around you. Widens and fades the same way."),
 "ink":("C: A fan in ink","The same soft fan in ink (navy by day, light ink at night) so it stands out from the sandy map in sunlight; the dot stays orange. Orange is no longer the only colour that means you."),
}
for k,(t,n) in looks.items(): pathlib.Path(f"heading-{k}.html").write_text(page(k,t,n))
