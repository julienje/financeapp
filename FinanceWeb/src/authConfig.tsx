export const msalConfig = {
    auth: {
        clientId: "1cfe66e3-db51-4082-93ad-0814bff72abf",
        authority: "https://login.microsoftonline.com/0829ce3c-dd9d-45a5-a7e4-b8fb69179085", // This is a URL (e.g. https://login.microsoftonline.com/{your tenant ID})
        redirectUri: "/",
    },
    cache: {
        cacheLocation: "sessionStorage", // This configures where your cache will be stored
        storeAuthStateInCookie: false, // Set this to "true" if you are having issues on IE11 or Edge
    }
};