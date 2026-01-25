import {StrictMode} from 'react';
import {createRoot} from 'react-dom/client';
import './index.css';
import App from './App';
import {createBrowserRouter} from "react-router";
import {RouterProvider} from "react-router/dom";
import ErrorPage from "./components/error-page";
import ShowWealth from "./components/show-wealth";
import ShowAccounts from "./components/show-accounts";
import AddBalances from "./components/add-balances";
import {MsalProvider} from "@azure/msal-react";
import {msalConfig} from "./authConfig";
import {PublicClientApplication} from "@azure/msal-browser";
import AddAccount from "./components/add-account";
import ShowTrend from "./components/show-trend";
import AddInvestment from "./components/add-investment.tsx";
import ShowProfit from "./components/show-profit.tsx";

const msalInstance = new PublicClientApplication(msalConfig);


const router = createBrowserRouter([
    {
        path: "/",
        element: <App/>,
        errorElement: <ErrorPage/>,
        children: [
            {
                path: "wealth",
                element: <ShowWealth/>
            }, {
                path: "trend",
                element: <ShowTrend/>
            },
            {
                path: "accounts",
                element: <ShowAccounts/>,
            },
            {
                path: "balances",
                element: <AddBalances/>
            },
            {
                path: "account",
                element: <AddAccount/>,
            },
            {
                path: "investment",
                element: <AddInvestment/>,
            },
            {
                path: "profit",
                element: <ShowProfit/>,
            }
        ],
    },
]);

createRoot(
    document.getElementById('root')!
).render(
    <StrictMode>
        <MsalProvider instance={msalInstance}>
            <RouterProvider router={router}/>
        </MsalProvider>
    </StrictMode>
);