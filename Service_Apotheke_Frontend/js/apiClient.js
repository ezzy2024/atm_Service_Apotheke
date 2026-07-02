// jshttps://serviceapotheke-api-830781040278.europe-west3.run.app/apiClient.js
const API_BASE_URL = "https://serviceapotheke-api-830781040278.europe-west3.run.app/api";

const ApiClient = {
    async request(endpoint, method = 'GET', body = null) {
        const url = `${API_BASE_URL}${endpoint}`;
        const options = {
            method,
            headers: {
                'Accept': 'application/json'
            },
            // CRITICAL: Forces the browser to send the HttpOnly JWT cookie
            credentials: 'include' 
        };

        const token = localStorage.getItem('userToken');
        if (token) {
            options.headers['Authorization'] = `Bearer ${token}`;
        }

        if (body) {
            options.headers['Content-Type'] = 'application/json';
            options.body = JSON.stringify(body);
        }

        try {
            const response = await fetch(url, options);
            
            if (response.status === 401) {
                console.warn("[Auth] Session invalid or cookie missing. Forcing logout.");
                if (typeof Auth !== 'undefined' && Auth.logout) {
                    Auth.logout();
                } else {
                    localStorage.clear();
                    window.location.replace("https://serviceapotheke.tech/login.html?logout=true");
                }
                throw new Error("Sitzung abgelaufen.");
            }

            const contentType = response.headers.get("content-type");
            const data = contentType && contentType.includes("application/json") 
                ? await response.json() 
                : await response.text();

            if (!response.ok) {
                throw new Error(data?.message || (typeof data === 'string' ? data : "Systemfehler"));
            }

            return data;
        } catch (error) {
            console.error(`[API Error] ${method} ${endpoint}:`, error);
            throw error;
        }
    },

    get(endpoint) { return this.request(endpoint, 'GET'); },
    post(endpoint, body) { return this.request(endpoint, 'POST', body); },
    put(endpoint, body) { return this.request(endpoint, 'PUT', body); },
    delete(endpoint) { return this.request(endpoint, 'DELETE'); }
};
