// Utility for making authenticated API calls with automatic 401 handling

export interface AuthenticatedFetchOptions extends RequestInit {
  onUnauthorized?: () => void;
}

/**
 * Wrapper around fetch that automatically handles 401 responses
 * by redirecting to sign-in and storing the current URL for redirect after login
 */
export async function authenticatedFetch(
  url: string,
  options: AuthenticatedFetchOptions = {}
): Promise<Response> {
  const { onUnauthorized, ...fetchOptions } = options;

  const response = await fetch(url, fetchOptions);

  if (response.status === 401) {
    // Store current URL for redirect after sign-in
    const currentPath = window.location.pathname + window.location.search;
    if (currentPath !== '/') {
      sessionStorage.setItem('redirectAfterLogin', currentPath);
    }

    // Call the unauthorized handler if provided
    if (onUnauthorized) {
      onUnauthorized();
    }

    throw new Error('Unauthorized - please sign in again');
  }

  return response;
}

/**
 * Get and clear the redirect URL stored after unauthorized access
 */
export function getAndClearRedirectUrl(): string | null {
  const redirectUrl = sessionStorage.getItem('redirectAfterLogin');
  if (redirectUrl) {
    sessionStorage.removeItem('redirectAfterLogin');
    return redirectUrl;
  }
  return null;
}
