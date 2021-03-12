namespace Hobbes.UniformData.Services

open Hobbes.Web.Routing
open Hobbes.Web
open Hobbes.Helpers
open Hobbes.Messaging.Broker
open Hobbes.Messaging

[<RouteArea ("/data", false)>]
module Data =
    let private cache = Cache.cache("uniform") 
    [<Get ("/read/%s")>]
    let read key =
        let uniformData =
           key
           |> cache.Get 
            
        match uniformData with
        Some uniformData ->
            200, uniformData.ToString()
        | None -> 
            404,sprintf "No data found for %s" key

    [<Post ("/update", true)>]
    let update dataAndKey =
        try
            let args =
                try
                    dataAndKey
                    |> Cache.CacheRecord.OfJson
                    |> Some
                with e ->
                    eprintfn "Failed to deserialization (%s). %s %s" dataAndKey e.Message e.StackTrace
                    None
            match args with
            Some args ->
                let key = args.CacheKey
                let data = args.Data
                Log.logf "updating cache with _id: %s" key
                try    
                    data
                    |> cache.InsertOrUpdate key args.DependsOn
                    Broker.Cache (Updated key)
                with e ->
                    Log.excf e "Failed to insert %s" (dataAndKey.Substring(0,min 500 dataAndKey.Length))
                200, "updated"
            | None -> 400,"Failed to parse payload"
        with e -> 
            Log.excf e "Couldn't update"
            500,"Internal server error"
    [<Get "/ping">]
    let ping () =
        200, "ping - UniformData"