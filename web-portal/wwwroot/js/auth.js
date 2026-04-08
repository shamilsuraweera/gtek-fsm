window.gtekAuth = {
    getStoredSession: function (key) {
        return window.localStorage.getItem(key);
    },
    setStoredSession: function (key, value) {
        window.localStorage.setItem(key, value);
    },
    clearStoredSession: function (key) {
        window.localStorage.removeItem(key);
    }
};
