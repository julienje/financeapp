import {useEffect, useState} from "react";
import {getWealth, WealthDto} from "../service";
import {useMsal} from "@azure/msal-react";
import {DatePicker} from "@mui/x-date-pickers/DatePicker";
import {LocalizationProvider} from '@mui/x-date-pickers/LocalizationProvider';
import {AdapterDayjs} from "@mui/x-date-pickers/AdapterDayjs";
import dayjs, {Dayjs} from "dayjs";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Accordion from "@mui/material/Accordion";
import AccordionSummary from "@mui/material/AccordionSummary";
import {AccordionDetails, CircularProgress} from "@mui/material";
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import {useTheme} from "@mui/material/styles";
import {dateTimeTemplate} from "../constants";

const ShowWealth = () => {
    const theme = useTheme();
    const [date, setDate] = useState<Dayjs>(dayjs());
    const {instance} = useMsal();
    const [wealth, setWealth] = useState<WealthDto>();
    const [loading, setLoading] = useState(false);
    useEffect(() => {
        if (date) {
            const controller = new AbortController();
            const result = async () => {
                setLoading(true);
                const data = await getWealth(controller.signal, instance, date);
                setWealth(data);
                setLoading(false);
            }
            result().catch(console.error);
            return () => {
                controller.abort();
            }
        }
    }, [date, instance]);

    const readerInfo = () => {
        if (loading) {
            return (
                <Box>
                    <CircularProgress/>
                </Box>
            );
        }
        return renderWealth();
    }

    const renderWealth = () => {
        const emptyDetails = (<Typography>There is no details</Typography>);
        const details = wealth?.Details.length === 0 ? emptyDetails : wealth?.Details.map(d =>
            (
                <Accordion key={d.AccountId} >
                    <AccordionSummary expandIcon={<ExpandMoreIcon/>}>
                        <Typography>{d.AccountName} - {d.AmountInChf} CHF</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        <Typography>
                            The account {d.AccountName} by {d.AccountCompany} had {d.AmountInChf} CHF
                            on {dayjs(d.CheckDate).format(dateTimeTemplate)}
                        </Typography>
                    </AccordionDetails>
                </Accordion>
            ));
        const exportDate = dayjs(wealth?.ExportDate).format(dateTimeTemplate);
        return (
            <Box>
                <Typography>
                    The wealth on {exportDate} is {wealth?.AmountInChf} CHF.
                </Typography>
                {details}
            </Box>
        );
    };

    return (
        <Box sx={{
            p: theme.spacing(1)
        }}>
            <h1>Get you actual wealth</h1>
            <form>
                <LocalizationProvider dateAdapter={AdapterDayjs}>
                    <DatePicker
                        label="Wealth at"
                        value={date}
                        onChange={(newValue) => setDate(newValue ?? dayjs())}
                    />
                </LocalizationProvider>
            </form>
            <div>
                {readerInfo()}
            </div>
        </Box>
    );
};

export default ShowWealth;