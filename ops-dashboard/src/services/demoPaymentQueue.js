const rails = ['EFT-RTGS', 'CARD-3DS', 'SWIFT'];
const deliveryStates = ['ENQUEUED', 'IN-FLIGHT (LOCKED)', 'SETTLED', 'RETRYING (2/3)', 'DLQ-QUARANTINED'];

export function makeDemoPayment(sequence, enqueuedAt) {
  const index = sequence % deliveryStates.length;
  return {
    idempotencyKey: 'idem_' + globalThis.crypto.randomUUID(),
    traceId: 'trace_' + globalThis.crypto.randomUUID(),
    amountMinor: 1250000 + (sequence % 8) * 900000,
    currency: 'ZAR',
    rail: rails[sequence % rails.length],
    state: deliveryStates[index],
    receiveCount: [0, 1, 1, 2, 3][index],
    maxReceives: 3,
    ingressAt: enqueuedAt - 15,
    enqueuedAt,
    pickedAt: index === 0 ? null : enqueuedAt + 80,
    stateAt: enqueuedAt + [0, 80, 160, 180, 220][index],
    messageGroupId: 'Shard-0' + (1 + sequence % 4),
    sequenceNumber: String(sequence),
    md5OfBody: null,
  };
}

export function queueHealth(rows, now) {
  const available = rows.filter(row => ['ENQUEUED', 'RETRYING (2/3)'].includes(row.state));
  const inFlight = rows.filter(row => row.state === 'IN-FLIGHT (LOCKED)').length;
  return {
    available: available.length,
    inFlight,
    oldestMs: available.length ? Math.max(0, now - Math.min(...available.map(row => row.enqueuedAt))) : null,
    dlq: rows.filter(row => row.state === 'DLQ-QUARANTINED').length,
  };
}

export const queueFilters = {
  'All Messages': () => true,
  'In-Flight Only': row => row.state === 'IN-FLIGHT (LOCKED)',
  'Retries > 1': row => Math.max(0, row.receiveCount - 1) > 1,
  'High Value (> R50,000)': row => row.currency === 'ZAR' && row.amountMinor > 5000000,
};

export function sanitizedPayload(row) {
  return {
    idempotencyKey: row.idempotencyKey,
    traceId: row.traceId,
    currency: row.currency,
    amountMinor: row.amountMinor,
    rail: row.rail,
    state: row.state,
    cardPan: row.rail === 'CARD-3DS' ? '••••••••4821' : null,
    accountNumber: row.rail !== 'CARD-3DS' ? '••••••••9012' : null,
    authorization: '••••••••',
  };
}
