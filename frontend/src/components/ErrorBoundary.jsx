import React from 'react';
import { AlertTriangle, RefreshCw, Home } from 'lucide-react';

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
    console.error(`[ErrorBoundary${this.props.name ? ` - ${this.props.name}` : ''}]`, error, errorInfo);
  }

  componentDidUpdate(prevProps) {
    // Auto-reset error state if route / key prop changes
    if (this.state.hasError && this.props.resetKey !== prevProps.resetKey) {
      this.setState({ hasError: false, error: null, errorInfo: null });
    }
  }

  handleRetry = () => {
    this.setState({ hasError: false, error: null, errorInfo: null });
  };

  handleGoHome = () => {
    window.location.href = '/';
  };

  render() {
    if (this.state.hasError) {
      const isAr = document.documentElement.dir === 'rtl' || document.documentElement.lang === 'ar';

      return (
        <div className="route-error-container">
          <div className="route-error-card">
            <div className="route-error-icon">
              <AlertTriangle size={28} />
            </div>

            <h2 className="route-error-title">
              {this.props.title || (isAr ? 'حدث خطأ غير متوقع في هذه الصفحة' : 'An unexpected error occurred on this page')}
            </h2>

            <p className="route-error-message">
              {this.state.error?.message || (isAr ? 'تعذر تحميل محتوى الصفحة بشكل صحيح. يمكنك المحاولة مرة أخرى أو العودة للرئيسية.' : 'Could not render page content. You can try again or return to dashboard.')}
            </p>

            {this.state.errorInfo && (
              <details className="route-error-details">
                <summary>
                  {isAr ? 'عرض التفاصيل التقنية للخطأ' : 'Show technical error details'}
                </summary>
                <pre className="route-error-stack">
                  {this.state.error?.stack}
                  {'\n'}
                  {this.state.errorInfo.componentStack}
                </pre>
              </details>
            )}

            <div className="route-error-actions">
              <button
                type="button"
                className="btn btn-primary"
                onClick={this.handleRetry}
              >
                <RefreshCw size={16} />
                {isAr ? 'إعادة المحاولة' : 'Try Again'}
              </button>

              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={this.handleGoHome}
              >
                <Home size={16} />
                {isAr ? 'العودة للرئيسية' : 'Go to Dashboard'}
              </button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
