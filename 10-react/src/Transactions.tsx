import { memo, useMemo, useState } from 'react';
import { useFetch } from './useFetch';

interface Txn { id: string; date: string; amount: number; type: 'D' | 'C'; }

const Row = memo(({ t }: { t: Txn }) => (
  <tr><td>{t.date}</td><td>{t.type}</td><td>{t.amount.toFixed(2)}</td></tr>
));

export function Transactions({ accountId }: { accountId: string }) {
  const { data, error, loading } = useFetch<Txn[]>(`/api/v1/accounts/${accountId}/transactions`);
  const [filter, setFilter] = useState<'ALL' | 'D' | 'C'>('ALL');

  // useMemo: recompute only when data or filter changes
  const rows = useMemo(() => (data ?? []).filter(t => filter === 'ALL' || t.type === filter), [data, filter]);

  if (loading) return <p>Loading…</p>;
  if (error) return <p role="alert">Error: {error}</p>;
  return (
    <>
      <select value={filter} onChange={e => setFilter(e.target.value as typeof filter)}>
        <option value="ALL">All</option><option value="D">Debits</option><option value="C">Credits</option>
      </select>
      <table><tbody>{rows.map(t => <Row key={t.id} t={t} />)}</tbody></table>
    </>
  );
}
