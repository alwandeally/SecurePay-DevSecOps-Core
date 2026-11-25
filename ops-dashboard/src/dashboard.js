import {ranges,readSnapshot} from './services/opsTelemetryService.js';
import {redactSensitiveFields} from './services/redactSensitiveFields.js';
const $=selector=>document.querySelector(selector);
const fmt=new Intl.NumberFormat('en-US',{maximumFractionDigits:0});
let range='15m', filter='All', paused=false, offline=false, generation=0, snapshot;
let stream=[];
const hiddenLines=new Set();
const panel=(content,classes='')=>'<article class="panel '+classes+'">'+content+'</article>';
const el=(tag,value,classes)=>{const node=document.createElement(tag);node.textContent=value;if(classes)node.className=classes;return node};
function choices(target,values,current,onChange){
  $(target).replaceChildren(...values.map(value=>{
    const button=el('button',value);
    button.setAttribute('aria-pressed',String(current===value));
    button.onclick=()=>{
      onChange(value);
      for(const choice of $(target).children)choice.setAttribute('aria-pressed',String(choice===button));
    };
    return button;
  }));
}
function plot(samples,lines,max,reference){
  const w=480,h=150,x=i=>40+i*(w-54)/(samples.length-1),y=v=>124-110*v/max;
  const grid=[0,max/2,max].map(v=>`<line x1="40" x2="466" y1="${y(v)}" y2="${y(v)}" stroke="#27272a" stroke-dasharray="3 3"/><text x="1" y="${y(v)+3}">${fmt.format(v)}</text>`).join('');
  const paths=lines.filter(line=>!hiddenLines.has(line.label)).map(line=>`<polyline fill="none" stroke="${line.color}" stroke-width="1.5" points="${samples.map((s,i)=>x(i)+','+y(line.value(s))).join(' ')}"/>`).join('');
  const ref=reference?`<line x1="40" x2="466" y1="${y(reference)}" y2="${y(reference)}" stroke="#fcd34d" stroke-dasharray="4 4"/>`:'';
  const wrapper=document.createElement('div');
  wrapper.innerHTML=`<svg class="chart" viewBox="0 0 ${w} ${h}" preserveAspectRatio="none" tabindex="0" aria-label="Chart. Use arrow keys to inspect samples."><g fill="#a1a1aa" font-size="10">${grid}<text x="40" y="145">${samples[0].time.slice(5,16).replace('T',' ')}</text><text x="466" y="145" text-anchor="end">${samples.at(-1).time.slice(5,16).replace('T',' ')} UTC</text></g>${ref}${paths}<line class="cursor" visibility="hidden" y1="14" y2="124" stroke="#a1a1aa"/></svg><div class="detail mono muted" aria-live="off">Hover or use arrow keys to inspect a sample.</div>`;
  const svg=wrapper.firstElementChild,cursor=svg.querySelector('.cursor');
  let index=0;
  const inspect=i=>{
    index=Math.min(samples.length-1,Math.max(0,i));
    cursor.setAttribute('x1',x(index));cursor.setAttribute('x2',x(index));cursor.setAttribute('visibility','visible');
    wrapper.lastElementChild.textContent=samples[index].time+' · '+lines.filter(l=>!hiddenLines.has(l.label)).map(l=>l.label+' '+fmt.format(l.value(samples[index]))).join(' · ');
  };
  svg.onpointermove=e=>{const bounds=svg.getBoundingClientRect();inspect(Math.round(((e.clientX-bounds.left)/bounds.width*w-40)/(w-54)*(samples.length-1)))};
  svg.onfocus=()=>inspect(index);
  svg.onkeydown=e=>{if(e.key==='ArrowLeft'||e.key==='ArrowRight'){e.preventDefault();inspect(index+(e.key==='ArrowLeft'?-1:1))}};
  return wrapper;
}
const line=(label,color,value)=>({label,color,value});
function renderCharts(samples){
  const [traffic,latency]=$('#charts').children;
  traffic.replaceChildren(el('h2','Traffic & Error Distribution · requests/s'));
  const seconds=(Date.parse(samples[1].time)-Date.parse(samples[0].time))/1000;
  traffic.append(plot(samples,[line('2xx','#6ee7b7',s=>s.success/seconds),line('4xx/5xx','#fda4af',s=>(s.clientErrors+s.serverErrors)/seconds)],1600));
  let legend=latency.querySelector('.toolbar');
  if(!legend){
    latency.append(el('h2','API Latency Percentiles (ms) · 100 ms target'));
    legend=el('div','','toolbar');latency.append(legend);
    for(const label of ['P50','P95','P99']){
      const button=el('button',label);
      button.setAttribute('aria-pressed','true');
      button.onclick=()=>{if(!hiddenLines.delete(label))hiddenLines.add(label);button.setAttribute('aria-pressed',String(!hiddenLines.has(label)));drawLatency(snapshot.samples)};
      legend.append(button);
    }
  }
  function drawLatency(data){
    if(legend.nextElementSibling)legend.nextElementSibling.remove();
    latency.append(plot(data,[line('P50','#a1a1aa',s=>s.p50),line('P95','#e4e4e7',s=>s.p95),line('P99','#fafafa',s=>s.p99)],110,100));
  }
  drawLatency(samples);
}
function render(data){
  for(const node of $('#charts').children)node.classList.remove('skeleton');
  const {samples,deployment:d}=data,last=samples.at(-1);
  const seconds=(Date.parse(samples[1].time)-Date.parse(samples[0].time))/1000;
  const total=last.success+last.clientErrors+last.serverErrors,tps=total/seconds;
  const budget=100*Math.max(0,1-(100-data.availability)/(100-data.slo));
  const spark=samples.map((s,i)=>`${i*5},${30-(s.success/seconds-1150)/10}`).join(' ');
  $('#metrics').innerHTML=[
    ['Throughput',fmt.format(tps)+' req/s',((tps/data.previousTps-1)*100).toFixed(1)+'% vs demo baseline <svg width="118" height="30" aria-hidden="true"><polyline fill="none" stroke="#a1a1aa" points="'+spark+'"/></svg>'],
    ['P99 Latency',last.p99+' ms','P50 '+last.p50+' ms · '+(100-last.p99)+' ms headroom<meter aria-label="P99 latency against 100 ms target" max="100" value="'+last.p99+'"></meter>'],
    ['Deployment & Security Gate','<span class="mono">'+d.sha+'</span>',`<span class="ok">${d.healthy}/${d.total} Tasks Healthy</span> · ${d.critical} Critical · ${d.high} High`],
    ['30-day SLO & Error Budget',data.availability.toFixed(3)+'%',`${(100*last.serverErrors/total).toFixed(2)}% 5xx · ${budget.toFixed(0)}% budget remaining`]
  ].map(([label,value,detail])=>panel(`<div class="label muted">${label}</div><span class="metric">${value}</span><div>${detail}</div>`)).join('');
  renderCharts(samples);
  $('#health').replaceChildren(...data.services.map(s=>{
    const card=el('article','','panel');
    card.append(el('strong','● '+s.name,'mono ok'),el('div',s.platform,'muted'),el('div',s.summary));
    for(const [label,value] of [['CPU',s.cpu],['Memory',s.memory]])if(value!==undefined){
      const meter=el('meter','');meter.max=100;meter.value=value;meter.setAttribute('aria-label',label+' utilization');
      card.append(el('small',label+' '+value+'%'),meter);
    }
    return card;
  }));
  if(!paused){stream=data.entries;renderEntries()}
}
function renderEntries(){
  const query=$('#search').value.trim().toLowerCase();
  const rows=stream.filter(e=>(e.traceId.includes(query)||e.ip.includes(query))&&(
    filter==='All'||(filter==='200 OK'&&e.status===200)||(filter==='4xx/5xx'&&e.status>=400)||(filter==='WAF Blocked'&&e.status===403)));
  const body=$('#entries');
  body.replaceChildren();
  if(!rows.length){const td=body.insertRow().insertCell();td.colSpan=7;td.textContent='No matching events.';return}
  for(const e of rows){
    const row=body.insertRow();
    const open=()=>{$('#inspector pre').textContent=JSON.stringify(redactSensitiveFields(e),null,2);$('#inspector').showModal()};
    row.onclick=open;
    row.insertCell().append(el('span',e.time,'mono'));
    const trace=row.insertCell(), inspect=el('button','…'+e.traceId.slice(-10),'mono'),copy=el('button','Copy');
    inspect.setAttribute('aria-label','Inspect trace '+e.traceId);
    copy.setAttribute('aria-label','Copy trace '+e.traceId);
    copy.onclick=async event=>{event.stopPropagation();try{await navigator.clipboard.writeText(e.traceId);$('#notice').textContent='Trace ID copied.'}catch{$('#notice').textContent='Clipboard unavailable. Open the inspector to copy the full ID.'}};
    trace.append(inspect,copy);
    for(const [value,style] of [[e.endpoint,'mono'],[e.service,'mono'],[e.posture,''],[e.latency+' ms','mono'],[e.status,'badge '+(e.status>=500?'bad':e.status>=400?'warn':'ok')]])row.insertCell().append(el('span',value,style));
  }
}
let loading=false;
async function refresh(){
  const request=++generation;
  const selectedRange=range;
  loading=true;
  $('#connection').textContent='Refreshing demo telemetry';
  try{
    const next=await readSnapshot(selectedRange,{disconnected:offline});
    if(request!==generation)return;
    snapshot=next;
    render(next);
    $('#warning').hidden=true;
    $('#connection').className='ok';
    $('#connection').textContent=paused?'● Demo stream paused':'● Demo stream live';
  }catch{
    if(request!==generation)return;
    $('#warning').hidden=false;
    $('#connection').className='warn';
    $('#connection').textContent='Telemetry degraded';
  }finally{if(request===generation)loading=false}
}
for(const [selector,count] of [['#metrics',4],['#charts',2],['#health',4]])$(selector).innerHTML=panel('','skeleton').repeat(count);
$('#entries').innerHTML='<tr><td colspan="7" class="skeleton"></td></tr>'.repeat(6);
choices('nav',Object.keys(ranges),range,value=>{range=value;void refresh()});
choices('#filters',['All','200 OK','4xx/5xx','WAF Blocked'],filter,value=>{filter=value;renderEntries()});
$('#search').oninput=renderEntries;
$('#refresh').onclick=refresh;
$('#retry').onclick=()=>{offline=false;void refresh()};
$('#disconnect').onclick=()=>{offline=true;void refresh()};
$('#pause').onclick=()=>{
  paused=!paused;
  $('#pause').textContent=paused?'Resume stream':'Pause stream';
  $('#pause').setAttribute('aria-pressed',String(paused));
  if(!paused&&snapshot){stream=snapshot.entries;renderEntries()}
  if(!offline)$('#connection').textContent=paused?'● Demo stream paused':'● Demo stream live';
};
void refresh();
setInterval(()=>{if(!loading&&!offline&&!$('#inspector').open&&!$('#entries').contains(document.activeElement)&&!$('#charts').contains(document.activeElement))void refresh()},5000);
