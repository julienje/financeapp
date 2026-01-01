import {IPublicClientApplication} from "@azure/msal-browser";
import {Dayjs} from "dayjs";

export interface WealthDto {
    AmountInChf: number
    ExportDate: string
    Details: WealthAccountDto[]
}

export interface TrendDto {
    AmountInChf: number
    CheckDate: string
}

export interface WealthAccountDto {
    AmountInChf: number
    AccountId: string
    AccountName: string
    AccountCompany: string
    CheckDate: string
}

export interface AccountDto {
    Id: string,
    Name: string,
    Company: string,
    Type: string,
    OpenDate: string,
    CloseDate: string
}

export interface AddBalanceDto {
    CheckDate: string
    AmountInChf: number
}

export interface AccountBalanceDto {
    Id: string
    AccountId: string
    CheckDate: string
    AmountInChf: number
}

export interface OpenAccountDto {
    Name: string
    Company: string
    Type: string
    OpenDate: string
}

export interface ErrorDto {
    error: string
}

export interface AddInvestmentDto {
    AmountInChf: number
    InvestmentDate: string
}

export interface InvestmentDto {
    Id: string
    CompanyName: string
    AmountInChf: number
    InvestmentDate: string
}

export interface CompanyDto {
    Name: string
}

export interface ProfitMoneyDto {
    InvestmentInChf: number
    WealthInChf: number
}

export interface CompanyProfitDto {
    Profit: ProfitMoneyDto
    Company: string
    Details: WealthAccountDto[]
}

export interface ProfitDto {
    Profit: ProfitMoneyDto
    Details: CompanyProfitDto[]
    ProfitDate: string
}


async function getAuthorizedHeaders(instance: IPublicClientApplication) {
    const {accessToken} = await instance.acquireTokenSilent({
        scopes: ['api://1cfe66e3-db51-4082-93ad-0814bff72abf/default'],
        account: instance.getAllAccounts()[0]
    });
    return new Headers({
        'Authorization': 'Bearer ' + accessToken
    });
}

export const getWealth = async (signal: AbortSignal, instance: IPublicClientApplication, date: Dayjs): Promise<WealthDto> => {
    const headers = await getAuthorizedHeaders(instance);
    const params = new URLSearchParams();
    params.set('date', date.toISOString());

    const request = await fetch(`${import.meta.env.VITE_APP_BACKEND_URL}/wealth?${params.toString()}`, {
        signal,
        headers
    });
    return await request.json() as WealthDto;
};

export const getTrend = async (signal: AbortSignal, instance: IPublicClientApplication): Promise<TrendDto[]> => {
    const headers = await getAuthorizedHeaders(instance);
    const request = await fetch(`${import.meta.env.VITE_APP_BACKEND_URL}/trend`, {signal, headers});
    return await request.json() as TrendDto[];
};

export const getAccounts = async (signal: AbortSignal, instance: IPublicClientApplication): Promise<AccountDto[]> => {
    const headers = await getAuthorizedHeaders(instance);
    const request = await fetch(`${import.meta.env.VITE_APP_BACKEND_URL}/accounts`, {signal, headers});
    return await request.json() as AccountDto[];
};

export const getBalanceForAccount = async (signal: AbortSignal, instance: IPublicClientApplication, accountId: string): Promise<AccountBalanceDto[]> => {
    const headers = await getAuthorizedHeaders(instance);
    const request = await fetch(`${import.meta.env.VITE_APP_BACKEND_URL}/accounts/${accountId}/balances`, {
        signal,
        headers
    });
    return await request.json() as AccountBalanceDto[];
};

export const addBalances = async (signal: AbortSignal, instance: IPublicClientApplication, accountId: string, balance: AddBalanceDto): Promise<AccountBalanceDto> => {
    const headers = await getAuthorizedHeaders(instance);
    headers.append('Content-Type', 'application/json');
    const requestOptions = {
        method: 'PUT',
        headers,
        body: JSON.stringify(balance),
        signal
    };
    const request = await fetch(`${import.meta.env.VITE_APP_BACKEND_URL}/accounts/${accountId}/balances/new`, requestOptions);
    return await request.json() as AccountBalanceDto;
};

export const addAccount = async (signal: AbortSignal, instance: IPublicClientApplication, newAccount: OpenAccountDto): Promise<AccountDto> => {
    const headers = await getAuthorizedHeaders(instance);
    headers.append('Content-Type', 'application/json');
    const requestOptions = {
        method: 'PUT',
        headers,
        body: JSON.stringify(newAccount),
        signal
    };
    const request = await fetch(`${import.meta.env.VITE_APP_BACKEND_URL}/accounts/new`, requestOptions);
    if (request.ok) {
        return await request.json() as AccountDto;
    }
    const error = await request.json() as ErrorDto;
    throw new Error(error.error);
};

export const addInvestment = async (signal: AbortSignal, instance: IPublicClientApplication, company: string, newInvestment: AddInvestmentDto): Promise<InvestmentDto> => {
    const headers = await getAuthorizedHeaders(instance);
    headers.append('Content-Type', 'application/json');
    const requestOptions = {
        method: 'PUT',
        headers,
        body: JSON.stringify(newInvestment),
        signal
    };
    const request = await fetch(`${import.meta.env.VITE_APP_BACKEND_URL}/investment/companies/${company}/new`, requestOptions);
    if (request.ok) {
        return await request.json() as InvestmentDto;
    }
    const error = await request.json() as ErrorDto;
    throw new Error(error.error);
};

export const getInvestmentCompany = async (signal: AbortSignal, instance: IPublicClientApplication): Promise<CompanyDto[]> => {
    const headers = await getAuthorizedHeaders(instance);
    headers.append('Content-Type', 'application/json');
    const requestOptions = {
        method: 'GET',
        headers,
        signal
    };
    const request = await fetch(`${import.meta.env.VITE_APP_BACKEND_URL}/investment/companies`, requestOptions);
    if (request.ok) {
        return await request.json() as CompanyDto[];
    }
    const error = await request.json() as ErrorDto;
    throw new Error(error.error);
};

export const getProfit = async (signal: AbortSignal, instance: IPublicClientApplication, date: Dayjs): Promise<ProfitDto> => {
    const headers = await getAuthorizedHeaders(instance);
    headers.append('Content-Type', 'application/json');
    const requestOptions = {
        method: 'GET',
        headers,
        signal
    };
    const params = new URLSearchParams();
    params.set('date', date.toISOString());
    const request = await fetch(`${import.meta.env.VITE_APP_BACKEND_URL}/investment/profit?${params.toString()}`, requestOptions);
    if (request.ok) {
        return await request.json() as ProfitDto;
    }
    const error = await request.json() as ErrorDto;
    throw new Error(error.error);
};

export const getInvestmentForCompany = async (signal: AbortSignal, instance: IPublicClientApplication, company: string): Promise<InvestmentDto[]> => {
    const headers = await getAuthorizedHeaders(instance);
    headers.append('Content-Type', 'application/json');
    const requestOptions = {
        method: 'GET',
        headers,
        signal
    };
    const request = await fetch(`${import.meta.env.VITE_APP_BACKEND_URL}/investment/companies/${company}`, requestOptions);
    if (request.ok) {
        return await request.json() as InvestmentDto[];
    }
    const error = await request.json() as ErrorDto;
    throw new Error(error.error);
};