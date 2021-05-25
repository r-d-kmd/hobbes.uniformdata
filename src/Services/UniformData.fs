namespace Hobbes.UniformData.Services

open Hobbes.Web.Routing
open Hobbes.Web
open Hobbes.Helpers
open Hobbes.Messaging.Broker
open Hobbes.Messaging

[<RouteArea ("/data", false)>]
module Data =
    type Record = FSharp.Data.JsonProvider<"""
        {
            "name":"kjh", 
            "meta" : {
                 "a":0,
                 "b": "something else"
            },
            "data" : [
                {
                    "some":"array"
                }
            ]
        }""">

    let private cache = Database.Database("uniformcache",Record.Parse,Log.loggerInstance)
    [<Get ("/read/%s")>]
    let read (key : string) =
        match key |> cache.TryGet  with
        Some uniformData ->
            200, System.String.Join(",", uniformData.Data
                                         |> Array.map(fun v -> v.JsonValue.ToString())
                 ) |> sprintf "[%s]"
        | None -> 
            404,sprintf "No data found for %s" key

    [<Post ("/update", true)>]
    let update (data : string) =
        Log.logf "Updating %s" (data.Substring(0,500))
        try
            let dataRecord = Record.Parse data
            assert(dataRecord.Name |> System.String.IsNullOrWhiteSpace |> not)
            try    
                cache.InsertOrUpdate(data) |> ignore
                Broker.Cache (Updated dataRecord.Name)
            with e ->
                Log.excf e "Failed to insert %s" (data.Substring(0,min 500 data.Length))
            200, "updated"
        
        with e -> 
            Log.excf e "Couldn't update"
            500,"Internal server error"
    [<Get "/ping">]
    let ping () =
        200, "ping - UniformData"