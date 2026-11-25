/** @typedef {{time:string,success:number,clientErrors:number,serverErrors:number,p50:number,p95:number,p99:number}} Sample */
/** @typedef {{name:string,platform:string,summary:string,cpu?:number,memory?:number}} ServiceHealth */
/** @typedef {{time:string,traceId:string,ip:string,endpoint:string,service:string,posture:string,latency:number,status:number,waf:string}} AuditLogEntry */
/** @typedef {{samples:Sample[],previousTps:number,services:ServiceHealth[],entries:AuditLogEntry[],deployment:{sha:string,healthy:number,total:number,critical:number,high:number},availability:number,slo:number}} TelemetrySnapshot */
export const ranges = {'15m':15,'1h':60,'6h':360,'24h':1440,'7d':10080};
const services = [
  {name:'auth-service',platform:'ECS Fargate',summary:'2/2 tasks · Circuit CLOSED',cpu:24,memory:41},
  {name:'tx-processor',platform:'ECS Fargate',summary:'4/4 tasks · Circuit CLOSED',cpu:52,memory:63},
  {name:'rds-ledger-primary',platform:'PostgreSQL Multi-AZ',summary:'12 ms writes · 142 connections'},
  {name:'edge-waf-alb',platform:'AWS WAF / ALB',summary:'99.4% pass · 18 blocked/hr'},
];
/** @returns {Promise<TelemetrySnapshot>} */
export async function readSnapshot(range, {disconnected=false, now=Date.now()}={}) {
  await new Promise(resolve=>setTimeout(resolve,350));
  if (disconnected) throw new Error('Demo telemetry unavailable');
  if (!Object.hasOwn(ranges,range)) throw new RangeError('Unsupported time range');
  const step=ranges[range]*60000/24;
  const samples=Array.from({length:24},(_,i)=>{
    const t=Math.floor(now/step)*step-(23-i)*step;
    const wave=Math.sin(t/step*.64);
    const success=Math.round((1330+wave*110)*step/1000);
    const clientErrors=Math.round(success*(.002+wave*.001));
    const serverErrors=Math.round(success*(.0001+wave*.00004));
    return {time:new Date(t).toISOString(),success,clientErrors,serverErrors,p50:Math.round(14+wave*3),p95:Math.round(27+wave*6),p99:Math.round(39+wave*9)};
  });
  const entries=Array.from({length:48},(_,i)=>{
    const n=Math.floor(now/5000)-i;
    const status=[200,401,200,429,503,200,403][((n%7)+7)%7];
    const auth=status===401;
    return {time:new Date(n*5000).toISOString(),traceId:n.toString(16).padStart(32,'0'),ip:'192.0.2.'+(1+i%64),
      endpoint:auth?'POST /demo/session':'GET /demo/transactions',service:auth?'auth-service':'tx-processor',
      posture:status===403?'WAF Blocked':status===429?'Rate-Limit Flagged':'Demo policy check',
      latency:status===503?114:16+i%24,status,waf:status===403?'DemoRateRule: BLOCK':'DemoRateRule: ALLOW'};
  });
  return {samples,previousTps:1330,services,entries,deployment:{sha:'c84f19a',healthy:6,total:6,critical:0,high:0},availability:99.985,slo:99.98};
}
