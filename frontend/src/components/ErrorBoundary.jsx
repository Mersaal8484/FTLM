import React from 'react';

class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null, errorInfo: null };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, errorInfo) {
    this.setState({ errorInfo });
    console.error('[ErrorBoundary]', error, errorInfo);
  }

  handleRetry = () => {
    this.setState({ hasError: false, error: null, errorInfo: null });
  };

  render() {
    if (this.state.hasError) {
      return (
        <div style={{
          padding: '24px',
          margin: '16px',
          border: '1px solid #e74c3c',
          borderRadius: '8px',
          backgroundColor: '#fdf0ef',
          fontFamily: 'Tajawal, sans-serif',
          direction: 'rtl',
        }}>
          <h3 style={{ color: '#e74c3c', margin: '0 0 8px' }}>
            حدث خطأ غير متوقع
          </h3>
          <p style={{ color: '#555', margin: '0 0 16px' }}>
            {this.state.error?.message || 'فشلت العملية'}
          </p>
          {this.state.errorInfo && (
            <details style={{ marginBottom: '16px' }}>
              <summary style={{ cursor: 'pointer', color: '#888' }}>
                تفاصيل الخطأ
              </summary>
              <pre style={{
                marginTop: '8px',
                padding: '12px',
                backgroundColor: '#f5f5f5',
                borderRadius: '4px',
                fontSize: '12px',
                direction: 'ltr',
                textAlign: 'left',
                overflow: 'auto',
              }}>
                {this.state.error?.stack}
                {'\n'}
                {this.state.errorInfo.componentStack}
              </pre>
            </details>
          )}
          <button
            onClick={this.handleRetry}
            style={{
              padding: '8px 16px',
              backgroundColor: '#3498db',
              color: '#fff',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontFamily: 'Tajawal, sans-serif',
            }}
          >
            إعادة المحاولة
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
