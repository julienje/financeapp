import {FormEvent, useEffect, useState} from "react";
import {addInvestment, CompanyDto, getInvestmentCompany} from "../service";
import {useMsal} from "@azure/msal-react";
import Box from "@mui/material/Box";
import {AdapterDayjs} from "@mui/x-date-pickers/AdapterDayjs";
import {DatePicker} from "@mui/x-date-pickers/DatePicker";
import {LocalizationProvider} from "@mui/x-date-pickers/LocalizationProvider";
import dayjs, {Dayjs} from "dayjs";
import {Button, CircularProgress, FormControl, InputLabel, MenuItem, Select, TextField} from "@mui/material";
import Typography from "@mui/material/Typography";
import {useTheme} from '@mui/material/styles';

const AddInvestment = () => {
    const theme = useTheme();
    const [amount, setAmount] = useState<string>('');
    const [company, setCompany] = useState<string>('');
    const [companies, setCompanies] = useState<CompanyDto[]>([]);
    const [status, setStatus] = useState<string>('');
    const [date, setDate] = useState<Dayjs | null>(dayjs());
    const [loading, setLoading] = useState(false);
    const {instance} = useMsal();
    useEffect(() => {
        const controller = new AbortController();
        const result = async () => {
            setLoading(true);
            const data = await getInvestmentCompany(controller.signal, instance);
            setCompanies(data);
            setLoading(false);
        }
        result().catch(console.error);
        return () => {
            controller.abort();
        }
    }, [instance]);
    const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        const controller = new AbortController();
        const amountInChf = Number(amount);
        if (company && date && !Number.isNaN(amountInChf)) {
            setStatus('Add investment in progress');
            addInvestment(controller.signal, instance, company, {
                AmountInChf: amountInChf,
                InvestmentDate: date.toISOString()
            })
                .then(() => setStatus('New Investement is created'))
                .catch((text: Error) => setStatus(`Error: ${text.message}`));
        }
    }
    const renderCompany = () => companies
        .map(a =>
            (
                <MenuItem value={a.Name} key={a.Name}>{a.Name}</MenuItem>
            ));

    const renderInvestment = () => {
        return (
            <Box>
                <form onSubmit={handleSubmit}>
                    <Box sx={{
                        p: theme.spacing(1)
                    }}>
                        <FormControl sx={{minWidth: 120}}>
                            <InputLabel id="company-label">Company</InputLabel>
                            <Select
                                labelId="company-label"
                                value={company}
                                onChange={(event) => setCompany(event.target.value)}
                            >
                                {renderCompany()}
                            </Select>
                        </FormControl>
                    </Box>
                    <Box sx={{
                        p: theme.spacing(1)
                    }}>
                        <LocalizationProvider dateAdapter={AdapterDayjs}>
                            <DatePicker
                                label="Invested at"
                                value={date}
                                onChange={(newValue) => setDate(newValue)}
                            />
                        </LocalizationProvider>
                    </Box>
                    <Box sx={{
                        p: theme.spacing(1)
                    }}>
                        <TextField
                            label="Amount in CHF"
                            value={amount}
                            onChange={(event) => setAmount(event.target.value)}/>
                    </Box>
                    <Box sx={{
                        p: theme.spacing(1)
                    }}>
                        <Button variant="contained" type="submit">Submit</Button>
                    </Box>
                </form>
                <Typography>
                    {status}
                </Typography>
            </Box>
        )
    }

    const readerInfo = () => {
        if (loading) {
            return (
                <Box>
                    <CircularProgress/>
                </Box>
            );
        }
        return renderInvestment();
    }

    return (
        <Box sx={{
            p: theme.spacing(1)
        }}>
            <h1>Add an Investment</h1>
            {readerInfo()}
        </Box>
    );

}

export default AddInvestment;