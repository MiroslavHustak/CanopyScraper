namespace Settings

open System

module Settings = 

    let [<Literal>] internal path = @"CanopyResults/canopy_results.json"                
   
    let internal url =
        match Environment.GetEnvironmentVariable "DOTNET_RUNNING_IN_CONTAINER" with
        | "true" -> @"http://canopy-api/" //For Docker networking
        | _      -> @"http://kodis.somee.com/api/"

    let [<Literal>] internal apiKeyTest = "test747646s5d4fvasfd645654asgasga654a6g13a2fg465a4fg4a3"
    let [<Literal>] internal pathToDriver = @"c:/temp/driver"
    let [<Literal>] internal urlKodis = @"https://kodis-files.s3.eu-central-1.amazonaws.com/"