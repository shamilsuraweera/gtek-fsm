window.gtekTheme = {
    getStoredTheme: function (key) {
        return window.localStorage.getItem(key);
    },
    setStoredTheme: function (key, value) {
        window.localStorage.setItem(key, value);
    },
    getSystemTheme: function () {
        return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    },
    applyTheme: function (theme) {
        document.documentElement.setAttribute("data-theme", theme);
    }
};
