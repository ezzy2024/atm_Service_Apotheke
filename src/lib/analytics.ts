// GA4 Measurement ID (Replace with real ID in production)
const GA_MEASUREMENT_ID = 'G-XXXXXXXXXX';

// Declare global gtag function for TypeScript
declare global {
  interface Window {
    dataLayer: any[];
    gtag: (...args: any[]) => void;
  }
}

/**
 * Injects the Google Analytics script into the DOM.
 * Should only be called after explicit user consent.
 */
export const initGoogleAnalytics = () => {
  // Prevent duplicate injection
  if (document.getElementById('ga-script')) return;

  // 1. Create and inject the external gtag script
  const script = document.createElement('script');
  script.id = 'ga-script';
  script.async = true;
  script.src = `https://www.googletagmanager.com/gtag/js?id=${GA_MEASUREMENT_ID}`;
  document.head.appendChild(script);

  // 2. Initialize dataLayer and gtag function
  window.dataLayer = window.dataLayer || [];
  window.gtag = function () {
    window.dataLayer.push(arguments);
  };

  // 3. Configure GA4
  window.gtag('js', new Date());
  window.gtag('config', GA_MEASUREMENT_ID, {
    page_path: window.location.pathname,
    anonymize_ip: true, // Privacy best practice
  });
};

/**
 * Tracks a custom event in Google Analytics.
 */
export const trackEvent = (eventName: string, eventParams: Record<string, any> = {}) => {
  // Only track if consent was given (gtag is defined)
  if (typeof window.gtag === 'function') {
    window.gtag('event', eventName, eventParams);
  } else {
    // For local development or when consent is denied
    console.debug(`[Analytics Mock] Event: ${eventName}`, eventParams);
  }
};
