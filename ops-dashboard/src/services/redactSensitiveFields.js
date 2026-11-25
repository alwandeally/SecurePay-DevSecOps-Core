// Defense in depth for parsed JSON. Redact at ingestion before sending real logs.
export function redactSensitiveFields(payload) {
  if (Array.isArray(payload)) return payload.map(redactSensitiveFields);
  if (payload === null || typeof payload !== 'object') return payload;
  return Object.fromEntries(Object.entries(payload).map(([field, value]) => {
    const key = field.toLowerCase().replace(/[^a-z0-9]/g, '');
    if (/authorization|password|passwd|secret|token|apikey|privatekey|accesskey|signingkey|credential|cookie|sessionid/.test(key) || key === 'key') {
      return [field, '••••••••'];
    }
    if (/cardnumber|creditcard|cardpan/.test(key) || key === 'pan') {
      return [field, typeof value === 'string' && /^\d(?:[ -]?\d){11,18}$/.test(value)
        ? '••••••••' + value.slice(-4) : '••••••••'];
    }
    if (/email|phone|fullname|address/.test(key) || ['ip','ipaddress','ssn','cvv','cvc'].includes(key)) {
      return [field, '••••••••'];
    }
    return [field, redactSensitiveFields(value)];
  }));
}
