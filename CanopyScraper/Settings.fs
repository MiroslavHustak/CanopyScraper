namespace Settings

open System
open System.IO

open Helpers

module Settings = 

    //let [<Literal>] internal path = @"CanopyResults/canopy_results.json"     
    
    let private isInKubernetes = 
        System.Environment.GetEnvironmentVariable "KUBERNETES_SERVICE_HOST"
        |> Option.ofNullEmptySpace
        |> Option.isSome

    let private isInContainer = 
        System.Environment.GetEnvironmentVariable "DOTNET_RUNNING_IN_CONTAINER"
        |> Option.ofNullEmptySpace
        |> Option.isSome
   
    // Kubernetes    
    let private basePath =
        Environment.GetEnvironmentVariable "APP_DATA_PATH"
        |> Option.ofNullEmptySpace
        |> Option.defaultValue AppContext.BaseDirectory  

    let private serviceRoot = Path.Combine(basePath, "canopy")      

    let internal path = Path.Combine(serviceRoot, "CanopyResults", "canopy_results.json")   
    
    let internal url =
        match isInKubernetes || isInContainer with
        | true  -> @"http://canopy-api/" //For Docker networking
        | false -> @"http://kodis.somee.com/api/"

    let [<Literal>] internal apiKeyTest = "test747646s5d4fvasfd645654asgasga654a6g13a2fg465a4fg4a3"
    let [<Literal>] internal urlKodis = @"https://kodis-files.s3.eu-central-1.amazonaws.com/"

    let internal pathToDriver =
        Environment.GetEnvironmentVariable "CHROMEDRIVER_PATH"
        |> Option.ofNullEmptySpace
        |> Option.defaultValue
            (
                match OperatingSystem.IsWindows() with
                | true  -> @"c:\temp\driver"
                | false -> @"/usr/bin"
            )