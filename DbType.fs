namespace FinanceApp

open MongoDB.Bson

module DbType =
    [<CLIMutable>]
    type AccountDb =
        { _id: ObjectId
          Name: string
          Company: string
          OpenDate: string
          CloseDate: string }
