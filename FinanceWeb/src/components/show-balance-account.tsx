import {useMsal} from "@azure/msal-react";
import {useEffect, useState} from "react";
import {AccountBalanceDto, getBalanceForAccount} from "../service";
import {Button, CircularProgress} from "@mui/material";
import Box from "@mui/material/Box";
import {DataGrid, GridColDef} from "@mui/x-data-grid";
import dayjs from "dayjs";

const ShowBalanceAccount = (props: { accountId: string }) => {
    const {instance} = useMsal();
    const [balances, setBalances] = useState<AccountBalanceDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [needLoading, setNeedLoading] = useState(false);
    const [loaded, setLoaded] = useState(false);

    const columns: GridColDef[] = [
        {
            field: 'CheckDate',
            headerName: 'Date',
            flex: 1,
            type: 'dateTime',
            valueGetter: (value) => dayjs(value).toDate(),
        },
        {
            field: 'AmountInChf',
            headerName: 'Amount',
            flex: 1
        }
    ];


    useEffect(() => {
        if (needLoading) {
            const controller = new AbortController();
            const result = async () => {
                setLoading(true);
                const data = await getBalanceForAccount(controller.signal, instance, props.accountId);
                setBalances(data);
                setLoading(false);
                setLoaded(true);
                setNeedLoading(false);
            }
            result().catch(console.error);
            return () => {
                controller.abort();
            }
        }
    }, [instance, needLoading, props.accountId]);

    const renderBalances = () => {
        if (!loaded) {
            return null;
        }
        if (loading) {
            return (
                <Box>
                    <CircularProgress/>
                </Box>
            );
        }

        const getRowId = (r: AccountBalanceDto): string => r.Id;

        return (
            <Box>
                <DataGrid
                    rows={balances}
                    columns={columns}
                    initialState={{
                        pagination: {
                            paginationModel: {
                                pageSize: 5,
                            },
                        },
                    }}
                    pageSizeOptions={[5]}
                    checkboxSelection
                    disableRowSelectionOnClick
                    getRowId={getRowId}
                />
            </Box>
        );
    };

    return (
        <Box>
            <Button onClick={() => {
                setNeedLoading(true);
            }}>{loaded ? 'Refresh' : 'Show'} balances?</Button>
            {renderBalances()}
        </Box>
    );
}
export default ShowBalanceAccount;