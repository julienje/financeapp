import {useEffect, useState} from "react";
import {getProfit, ProfitDto} from "../service";
import {useMsal} from "@azure/msal-react";
import Box from "@mui/material/Box";
import {AccordionDetails, CircularProgress} from "@mui/material";
import Typography from "@mui/material/Typography";
import {useTheme} from '@mui/material/styles';
import dayjs, {Dayjs} from "dayjs";
import {dateTimeTemplate} from "../constants.ts";
import AccordionSummary from "@mui/material/AccordionSummary";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import Accordion from "@mui/material/Accordion";
import {LocalizationProvider} from "@mui/x-date-pickers/LocalizationProvider";
import {AdapterDayjs} from "@mui/x-date-pickers/AdapterDayjs";
import {DatePicker} from "@mui/x-date-pickers/DatePicker";

const ShowProfit = () => {
    const theme = useTheme();
    const [profit, setProfit] = useState<ProfitDto | null>(null);
    const [date, setDate] = useState<Dayjs>(dayjs());
    const [loading, setLoading] = useState(false);
    const {instance} = useMsal();
    useEffect(() => {
        const controller = new AbortController();
        const result = async () => {
            setLoading(true);
            const data = await getProfit(controller.signal, instance, date);
            setProfit(data);
            setLoading(false);
        }
        result().catch(console.error);
        return () => {
            controller.abort();
        }
    }, [date, instance]);


    const renderInfo = () => {
        if (!profit) {
            return (
                <Box>
                    <Box sx={{
                        p: theme.spacing(1)
                    }}>
                        <Typography>There is no details</Typography>
                    </Box>
                </Box>
            )
        }

        const profitDate = dayjs(profit.ProfitDate).format(dateTimeTemplate);
        const companies = profit.Details.map(d => {
            const balanceDetails = d.Details.map(bd => {
                const accountCheckDate = dayjs(bd.CheckDate).format(dateTimeTemplate);
                const percentage = Math.round((bd.AmountInChf * 100.0) / d.Profit.WealthInChf);
                return (
                    <Typography key={bd.AccountId}>
                        On {accountCheckDate} the account {bd.AccountName} has {bd.AmountInChf} CHF
                        representing {percentage}
                    </Typography>
                );
            });
            return (
                <Accordion key={d.Company}>
                    <AccordionSummary expandIcon={<ExpandMoreIcon/>}>
                        <Typography>On {d.Company} the profit is {d.Profit.WealthInChf} CHF
                            for {d.Profit.InvestmentInChf} CHF invested
                            ({d.Profit.WealthInChf - d.Profit.InvestmentInChf} CHF).</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        {balanceDetails}
                    </AccordionDetails>
                </Accordion>
            );
        });
        return (
            <Box>
                <Typography>
                    At {profitDate} the profit is {profit.Profit.WealthInChf} CHF
                    for {profit.Profit.InvestmentInChf} CHF invested
                    ({profit.Profit.WealthInChf - profit.Profit.InvestmentInChf} CHF).
                </Typography>
                {companies}
            </Box>
        )
    }

    const renderLoading = () => {
        if (loading) {
            return (
                <Box>
                    <CircularProgress/>
                </Box>
            );
        }
        return renderInfo();
    }

    return (
        <Box sx={{
            p: theme.spacing(1)
        }}>
            <h1>Profit</h1>
            <form>
                <LocalizationProvider dateAdapter={AdapterDayjs}>
                    <DatePicker
                        label="Profit at"
                        value={date}
                        onChange={(newValue) => setDate(newValue ?? dayjs())}
                    />
                </LocalizationProvider>
            </form>
            {renderLoading()}
        </Box>
    );

}

export default ShowProfit;