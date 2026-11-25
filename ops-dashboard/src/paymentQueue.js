import {makeDemoPayment, queueHealth, queueFilters, sanitizedPayload} from './services/demoPaymentQueue.js';

const $ = id => document.getElementById(id);
const add = (parent, tag, text, className) => {
  const node = document.createElement(tag);
  node.textContent = text;
  if (className) node.className = className;
  parent.append(node);
  return node;
};
const limit = 150, slaMs = 5000;
let sequence = 0, live = true, pending = [], dropped = 0;
let filter = 'All Messages', shownAt = Date.now();
const batch = now => Array.from({length: 5}, (_, i) => makeDemoPayment(sequence++, now - 300 - i * 80));
let snapshot = batch(shownAt);
const filtered = () => snapshot.filter(queueFilters[filter]);
const age = (row, at) => Math.max(0, at - row.enqueuedAt);
const utc = value => new Date(value).toISOString();
const truncate = value => value.slice(0, 13) + '…' + value.slice(-4);
const money = new Intl.NumberFormat('en-ZA', {minimumFractionDigits: 2, maximumFractionDigits: 2});
const tools = $('queue-tools'), audit = $('queue-audit'), flush = $('queue-flush');

const dialog = add(document.body, 'dialog', '');
dialog.id = 'queue-peek';
dialog.setAttribute('aria-label', 'Message Peek');
add(dialog, 'button', 'Close').onclick = () => dialog.close();
add(dialog, 'h2', 'Message Peek · Read-Only');
add(dialog, 'p', 'NON-DESTRUCTIVE PEEK MODE — This reads a local snapshot only. No broker calls, ReceiveCount increments, or visibility changes.');
const timeline = add(dialog, 'dl', '');
const tabs = add(dialog, 'div', '', 'toolbar');
const payload = add(tabs, 'button', 'Sanitized JSON Payload');
const attributes = add(tabs, 'button', 'SQS / Broker Attributes');
const json = add(dialog, 'pre', '', 'mono');
const reason = 'Disabled: simulated SecOps-ReadOnly role. No send or delete capability.';
for (const label of ['Re-drive to Main Queue', 'Purge Message']) {
  const wrapper = add(dialog, 'span', '');
  wrapper.title = reason;
  add(wrapper, 'button', label).disabled = true;
}
add(dialog, 'p', reason);

function peek(row, showTrace = false) {
  timeline.replaceChildren();
  const steps = [
    ['1. API Gateway ingress & WAF check', row.ingressAt],
    ['2. Enqueued to FIFO partition ' + row.messageGroupId, row.enqueuedAt],
    ['3. Consumer pickup · tx-processor-02', row.pickedAt],
    ['4. Last recorded state · ' + row.state, row.stateAt],
  ];
  for (const [label, time] of steps) {
    add(timeline, 'dt', label);
    add(timeline, 'dd', time === null ? 'Not observed' : utc(time), 'mono');
  }
  const show = (selected, value) => {
    for (const tab of [payload, attributes]) tab.setAttribute('aria-pressed', String(tab === selected));
    json.textContent = JSON.stringify(value, null, 2);
  };
  payload.onclick = () => show(payload, sanitizedPayload(row));
  attributes.onclick = () => show(attributes, {
    MessageGroupId: row.messageGroupId, SequenceNumber: row.sequenceNumber,
    MD5OfBody: row.md5OfBody, ReceiveCount: row.receiveCount, CorrelationId: row.traceId,
  });
  (showTrace ? attributes : payload).onclick();
  dialog.showModal();
}

function health() {
  const target = $('queue-health');
  target.replaceChildren();
  const {available, inFlight, oldestMs, dlq} = queueHealth(snapshot, shownAt);
  for (const [title, value, detail] of [
    ['Queue depth', (available + inFlight) + ' msgs', `Available: ${available} · In-flight: ${inFlight}`],
    ['Oldest available message', oldestMs === null ? '—' : oldestMs + ' ms', oldestMs > slaMs ? 'Elevated backpressure · 5,000 ms demo target' : 'Normal backpressure · 5,000 ms demo target'],
    ['Processing velocity', '5 in / 3 out msg/s', 'Illustrative rates · not measured'],
    ['DLQ depth', dlq + ' in DLQ', 'Quarantined in retained snapshot'],
  ]) {
    const card = add(target, 'article', '', 'panel');
    add(card, 'div', title);
    const metric = add(card, 'strong', value, 'mono');
    if (title === 'DLQ depth') metric.classList.add(dlq ? 'warn' : 'ok');
    add(card, 'div', detail, 'muted');
  }
}
function render() {
  health();
  const body = $('queue-rows');
  body.replaceChildren();
  for (const row of filtered()) {
    const tr = body.insertRow();
    tr.onclick = () => peek(row);
    const identity = tr.insertCell();
    add(identity, 'button', truncate(row.idempotencyKey), 'mono').onclick = event => {
      event.stopPropagation();
      peek(row);
    };
    add(identity, 'button', 'Copy').onclick = async event => {
      event.stopPropagation();
      try {
        await navigator.clipboard.writeText(row.idempotencyKey);
        audit.textContent = 'Idempotency key copied.';
      } catch {
        audit.textContent = 'Clipboard unavailable. The full key is in the inspector.';
      }
    };
    add(tr.insertCell(), 'button', truncate(row.traceId), 'mono').onclick = event => {
      event.stopPropagation();
      peek(row, true);
    };
    add(tr, 'td', row.messageGroupId, 'mono');
    add(tr, 'td', row.rail, 'mono');
    add(tr, 'td', row.currency + ' ' + money.format(row.amountMinor / 100), 'queue-amount');
    const tone = row.state.startsWith('DLQ') ? 'bad' : row.state === 'SETTLED' ? 'ok' :
      row.state.startsWith('RETRY') ? 'warn' : 'muted';
    add(tr.insertCell(), 'span', row.state, 'badge ' + tone);
    add(tr, 'td', `Attempt ${row.receiveCount}/${row.maxReceives} · ${age(row, shownAt)} ms ago`, 'mono');
  }
  if (!body.rows.length) add(body.insertRow(), 'td', 'No matching messages.').colSpan = 7;
}
function updateBufferLabel() {
  flush.hidden = live;
  flush.disabled = !pending.length;
  flush.textContent = `Pause active — ${pending.length} buffered${dropped ? ` · ${dropped} dropped` : ''} (click to flush)`;
}
function accept(now) {
  snapshot = [...pending, ...snapshot].slice(0, limit);
  pending = [];
  dropped = 0;
  shownAt = now;
  render();
}
const liveButton = $('queue-live');
liveButton.onclick = () => {
  live = !live;
  liveButton.setAttribute('aria-pressed', String(live));
  liveButton.textContent = live ? 'Live Tail' : 'Live Tail paused';
  if (live) accept(Date.now());
  updateBufferLabel();
};
flush.onclick = () => {
  accept(Date.now());
  updateBufferLabel();
};
const chips = Object.keys(queueFilters).map(name => {
  const button = add(tools, 'button', name);
  button.setAttribute('aria-pressed', String(filter === name));
  button.onclick = () => {
    filter = name;
    for (const chip of chips) chip.setAttribute('aria-pressed', String(chip === button));
    render();
  };
  return button;
});
$('queue-export').onclick = () => {
  const exportedAt = utc(Date.now());
  const rows = filtered().map(row => ({
    ...sanitizedPayload(row), messageGroupId: row.messageGroupId,
    receiveCount: row.receiveCount, enqueuedAt: utc(row.enqueuedAt),
  }));
  const blob = new Blob([JSON.stringify({simulated: true, exportedAt, snapshotAt: utc(shownAt), filter, rows}, null, 2)], {type: 'application/json'});
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = 'securepay-queue-snapshot-' + exportedAt.slice(0, 16).replace(':', '-') + 'Z.json';
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
  audit.textContent = `Simulated read audit · ${exportedAt} · Export requested for ${rows.length} rows · ${filter}`;
};
render();
setInterval(() => {
  const now = Date.now();
  pending.unshift(...batch(now));
  dropped += Math.max(0, pending.length - limit);
  pending.length = Math.min(pending.length, limit);
  if (live && !dialog.open && !$('queue-rows').contains(document.activeElement)) accept(now);
  updateBufferLabel();
}, 1000);
