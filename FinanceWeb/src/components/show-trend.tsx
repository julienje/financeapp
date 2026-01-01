import {useEffect, useState} from "react";
import {getTrend, TrendDto} from "../service";
import {useMsal} from "@azure/msal-react";
import dayjs from "dayjs";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import {CircularProgress} from "@mui/material";
import {useTheme} from "@mui/material/styles";
import {LineChart} from "@mui/x-charts";

const ShowTrend = () => {
    const theme = useTheme();
    const {instance} = useMsal();
    const [trend, setTrend] = useState<TrendDto[]>([]);
    const [loading, setLoading] = useState(false);
    useEffect(() => {
        const controller = new AbortController();
        const result = async () => {
            setLoading(true);
            const data = await getTrend(controller.signal, instance);
            setTrend(data);
            setLoading(false);
        }
        result().catch(console.error);
        return () => {
            controller.abort();
        }
    }, [instance]);

    const readerInfo = () => {
        if (loading) {
            return (
                <Box>
                    <CircularProgress/>
                </Box>
            );
        }
        return renderTrend();
    }

    const renderTrend = () => {
        const emptyDetails = (<Typography>There is no trend</Typography>);
        const chartData = trend?.map(t => {
            return {day: dayjs(t.CheckDate).toDate(), value: t.AmountInChf}
        });
        const moneyChart = !chartData ? emptyDetails : (
            <LineChart
                xAxis={[
                    {
                        scaleType: 'time',
                        dataKey: 'day'
                    },
                ]}
                series={[{dataKey: 'value'}]}
                dataset={chartData}
                width={500}
                height={300}
            />)
        const evolData = trend?.map((e, i, a) => {
            if (i === 0) {
                return {day: dayjs(e.CheckDate).toDate(), value: null};
            }
            return {day: dayjs(e.CheckDate).toDate(), value: e.AmountInChf - a[i - 1].AmountInChf}
        }).filter(e => e.value != null);
        const evolChart = !evolData ? emptyDetails : (
            <LineChart
                xAxis={[
                    {
                        scaleType: 'time',
                        dataKey: 'day'
                    },
                ]}
                series={[{dataKey: 'value'}]}
                dataset={evolData}
                width={500}
                height={300}
            />)
        return (
            <Box>
                <Typography>
                    Here the trend of your wealth
                </Typography>
                {moneyChart}
                <Typography>
                    Here the gain or loss
                </Typography>
                {evolChart}
            </Box>
        );
    };

    return (
        <Box sx={{
            p: theme.spacing(1)
        }}>
            <h1>Show your trend</h1>
            <div>
                {readerInfo()}
            </div>
        </Box>
    );
};

export default ShowTrend;