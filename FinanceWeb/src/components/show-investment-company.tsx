import {useMsal} from "@azure/msal-react";
import {useEffect, useState} from "react";
import {getInvestmentForCompany, InvestmentDto} from "../service";
import {Button, CircularProgress} from "@mui/material";
import Box from "@mui/material/Box";
import {DataGrid, GridColDef} from "@mui/x-data-grid";
import dayjs from "dayjs";

const ShowInvestmentCompany = (props: { company: string }) => {
    const {instance} = useMsal();
    const [investments, setInvestments] = useState<InvestmentDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [needLoading, setNeedLoading] = useState(false);
    const [loaded, setLoaded] = useState(false);

    const columns: GridColDef[] = [
        {
            field: 'InvestmentDate',
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
                const data = await getInvestmentForCompany(controller.signal, instance, props.company);
                setInvestments(data);
                setLoading(false);
                setLoaded(true);
                setNeedLoading(false);
            }
            result().catch(console.error);
            return () => {
                controller.abort();
            }
        }
    }, [instance, needLoading, props.company]);

    const renderInvestments = () => {
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

        const getRowId = (r: InvestmentDto): string => r.Id;

        return (
            <Box>
                <DataGrid
                    rows={investments}
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
            }}>{loaded ? 'Refresh' : 'Show'} investments?</Button>
            {renderInvestments()}
        </Box>
    );
}
export default ShowInvestmentCompany;