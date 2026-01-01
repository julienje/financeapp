import {FormEvent, useState} from "react";
import Box from "@mui/material/Box";
import {LocalizationProvider} from "@mui/x-date-pickers/LocalizationProvider";
import {AdapterDayjs} from "@mui/x-date-pickers/AdapterDayjs";
import {DatePicker} from "@mui/x-date-pickers/DatePicker";
import {Button, FormControl, InputLabel, MenuItem, Select, TextField} from "@mui/material";
import dayjs, {Dayjs} from "dayjs";
import {addAccount} from "../service";
import {useMsal} from "@azure/msal-react";
import {useTheme} from "@mui/material/styles";
import Typography from "@mui/material/Typography";

const AddAccount = () => {
    const theme = useTheme();
    const [name, setName] = useState<string>('');
    const [company, setCompany] = useState<string>('');
    const [type, setType] = useState<string>('Unknown');
    const [status, setStatus] = useState<string>('');
    const [date, setDate] = useState<Dayjs | null>(dayjs());
    const {instance} = useMsal();
    const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        const controller = new AbortController();
        if (name && company && date) {
            setStatus('New Account in progress');
            addAccount(controller.signal, instance, {
                Name: name,
                Company: company,
                OpenDate: date.toISOString(),
                Type: type
            })
                .then(() => setStatus('New Account is created'))
                .catch((text: Error) => setStatus(`Error: ${text.message}`));
        }
    }

    return (
        <Box>
            <form onSubmit={handleSubmit}>
                <h1>Add a new account</h1>
                <Box sx={{
                    p: theme.spacing(1)
                }}>
                    <TextField
                        label="Name"
                        value={name}
                        onChange={(event) => setName(event.target.value)}/>
                </Box>
                <Box sx={{
                    p: theme.spacing(1)
                }}>
                    <TextField
                        label="Company"
                        value={company}
                        onChange={(event) => setCompany(event.target.value)}/>
                </Box>
                <Box sx={{
                    p: theme.spacing(1)
                }}>
                    <FormControl>
                        <InputLabel id="type-label">Type</InputLabel>
                        <Select
                            labelId="type-label"
                            id="type-select"
                            value={type}
                            label="Age"
                            onChange={(event) => setType(event.target.value)}
                        >
                            <MenuItem value={"Unknown"}>Unknown</MenuItem>
                            <MenuItem value={"3A"}>3A</MenuItem>
                            <MenuItem value={"ETF"}>ETF</MenuItem>
                            <MenuItem value={"AvailableToTrade"}>Available To Trade</MenuItem>
                        </Select>
                    </FormControl>
                </Box>
                <Box sx={{
                    p: theme.spacing(1)
                }}>
                    <LocalizationProvider dateAdapter={AdapterDayjs}>
                        <DatePicker
                            label="Open at"
                            value={date}
                            onChange={(newValue) => setDate(newValue)}
                        />
                    </LocalizationProvider>
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
    );
}

export default AddAccount;