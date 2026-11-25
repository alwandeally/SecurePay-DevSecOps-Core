const el = id => document.getElementById('ops-' + id);
let token = '', page = 1, totalPages = 0, status = '', request;
const rows = el('records');
const message = text => { el('message').textContent = text; };
function controls(busy = false) {
  el('prev').disabled = busy || !token || page <= 1;
  el('next').disabled = busy || !token || page >= totalPages;
}
function clear(text) {
  request?.abort();
  request = undefined;
  token = '';
  el('token').value = '';
  page = 1;
  totalPages = 0;
  rows.replaceChildren();
  controls();
  message(text);
}
async function load(target) {
  if (!token) { message('Enter an Operations or Administrator access token.'); return; }
  request?.abort();
  const active = new AbortController();
  request = active;
  controls(true);
  rows.replaceChildren();
  message('Loading transactions…');
  const timeout = setTimeout(() => active.abort(), 12000);
  try {
    const query = new URLSearchParams({page: String(target), pageSize: '25'});
    if (status) query.set('status', status);
    const response = await fetch('/api/operations/transactions?' + query, {
      headers: {Authorization: 'Bearer ' + token, Accept: 'application/json'},
      redirect: 'error', cache: 'no-store', credentials: 'omit', signal: active.signal,
    });
    if (request !== active) return;
    if (response.status === 401 || response.status === 403) {
      clear(response.status === 401 ? 'Token rejected or expired. Obtain a new token.' : 'Access denied. Operations or Administrator role required.');
      return;
    }
    if (!response.ok) throw new Error('Request failed');
    const data = await response.json();
    if (request !== active) return;
    if (!Array.isArray(data.items) || !Number.isSafeInteger(data.totalPages) || data.totalPages < 0) throw new Error('Invalid result');
    const fragment = document.createDocumentFragment();
    for (const item of data.items) {
      const tr = document.createElement('tr');
      for (const value of [new Date(item.createdAtUtc).toISOString(), item.reference, item.transactionType, item.amount, item.currency, item.status]) {
        tr.insertCell().textContent = String(value ?? '—');
      }
      fragment.append(tr);
    }
    rows.replaceChildren(fragment);
    page = target;
    totalPages = data.totalPages;
    message(data.items.length ? `Page ${page} of ${totalPages}` : 'No transactions on this page.');
  } catch {
    if (request === active) {
      totalPages = 0;
      message(active.signal.aborted ? 'Request timed out. Try again.' : 'Could not load transactions. Check the local Transactions API and retry.');
    }
  } finally {
    clearTimeout(timeout);
    if (request === active) { request = undefined; controls(); }
  }
}
el('load').onclick = () => {
  const entered = el('token').value.trim();
  el('token').value = '';
  if (entered) token = entered;
  status = el('status').value;
  void load(1);
};
el('prev').onclick = () => void load(page - 1);
el('next').onclick = () => void load(page + 1);
el('clear').onclick = () => clear('Session cleared.');
window.addEventListener('pagehide', () => clear('Session cleared.'));
