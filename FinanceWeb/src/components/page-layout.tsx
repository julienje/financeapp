import {Outlet} from "react-router";
import Link from "@mui/material/Link";
import Box from "@mui/material/Box";
import {createTheme, ThemeProvider} from "@mui/material/styles";
import CssBaseline from "@mui/material/CssBaseline";

const theme = createTheme({
    palette: {
        mode: 'dark',
    },
});

const PageLayout = () => {
    return (
        <ThemeProvider theme={theme}>
            <CssBaseline/>

            <Box>
                <Box sx={{
                    display: 'flex',
                    backgroundColor: 'primary',
                    '& > a': {
                        padding: theme.spacing(1)
                    },

                }}>
                    <Link href={`/wealth`}>Wealth</Link>
                    <Link href={`/trend`}>Trend</Link>
                    <Link href={`/profit`}>Profit</Link>
                    <Link href={`/balances`}>Balance</Link>
                    <Link href={`/investment`}>Investment</Link>
                    <Link href={`/accounts`}>Accounts</Link>
                    <Link href={`/account/new`}>New account</Link>
                </Box>
                <Box sx={{
                    p: theme.spacing(1)
                }}>
                    <Outlet/>
                </Box>
            </Box>
        </ThemeProvider>
    );
};

export default PageLayout;
