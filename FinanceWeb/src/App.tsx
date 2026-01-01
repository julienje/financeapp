import './App.css';
import {AuthenticatedTemplate, UnauthenticatedTemplate, useMsalAuthentication} from "@azure/msal-react";
import {InteractionType} from "@azure/msal-browser";
import PageLayout from "./components/page-layout";

const App = () => {
    useMsalAuthentication(InteractionType.Redirect);
    return (
        <div>
            <AuthenticatedTemplate>
                <PageLayout/>
            </AuthenticatedTemplate>
            <UnauthenticatedTemplate>
                <p>You are not signed in! Please sign in.</p>
            </UnauthenticatedTemplate>
        </div>
    );
};

export default App;
