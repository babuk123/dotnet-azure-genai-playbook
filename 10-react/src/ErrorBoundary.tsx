import { Component, ReactNode } from 'react';

// Error boundaries must be class components: they catch render errors in their children
export class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false };
  static getDerivedStateFromError() { return { failed: true }; }
  componentDidCatch(error: Error) { console.error('UI error', error); }
  render() { return this.state.failed ? <p role="alert">Something went wrong.</p> : this.props.children; }
}
