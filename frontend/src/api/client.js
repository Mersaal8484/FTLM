import axios from 'axios';

const client = axios.create({
  // Empty baseURL - pages include /api/ in their paths
  // e.g. client.get('/api/oils') works correctly
  baseURL: import.meta.env.VITE_API_URL || '',
  timeout: 30000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
});

let sessionUserUpdateHandler = null;
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) reject(error);
    else resolve(token);
  });
  failedQueue = [];
};

/** App registers this to keep currentUser in sync after token refresh or /auth/me */
export function setSessionUserUpdateHandler(handler) {
  sessionUserUpdateHandler = handler;
}

function applySessionUser(user) {
  if (user && sessionUserUpdateHandler) {
    sessionUserUpdateHandler(user);
  }
}

/** Re-fetch profile + permissions from server (e.g. after role edits) */
export async function refreshSessionUser() {
  const res = await client.get('/api/auth/me');
  applySessionUser(res.data);
  return res.data;
}

client.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (!originalRequest || error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    if (['/api/auth/login', '/api/auth/login/2fa', '/api/auth/logout'].includes(originalRequest.url)) {
      return Promise.reject(error);
    }

    if (originalRequest.url === '/api/auth/refresh') {
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
      return Promise.reject(error);
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      }).then(() => {
        return client(originalRequest);
      });
    }

    originalRequest._retry = true;
    isRefreshing = true;

    try {
      await axios.post(
        `${client.defaults.baseURL}/api/auth/refresh`,
        null,
        { withCredentials: true }
      );
      processQueue(null);
      return client(originalRequest);
    } catch (refreshError) {
      processQueue(refreshError);
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

export default client;
