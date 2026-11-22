import { ErrorBoundary } from './ErrorBoundary';
import { Transactions } from './Transactions';

export default function App() {
  return (
    <ErrorBoundary>
      <h1>Account activity</h1>
      <Transactions accountId="ACC-1" />
    </ErrorBoundary>
  );
}
