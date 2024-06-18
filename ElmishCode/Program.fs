module MainProgram

open Elmish
open Elmish.WPF
open SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries
open Views
open System

type Model = {
    DockedModels: ChartPage.Model array
    GraphDataToDispose: UniformHeatmapDataSeries<double,double,int16> array
}

type Msg =
    | AddDockedModel
    | ChartPageMsg of Guid * ChartPage.Msg
    | DisposeGraphData
    | Error of exn

let garbageCollect () =
    printfn "Program: Garbage Collecting..."
    GC.Collect()
    printfn "Program: Garbage Collection complete"

let setupModelCmd () =
    let id = Guid.NewGuid()
    let m, cmd = ChartPage.init (id)
    m,
    Cmd.map (fun c -> ChartPageMsg(id, c)) cmd

let init(): Model*Cmd<Msg> = 
    {
        DockedModels = [||]
        GraphDataToDispose = [||]
    }, Cmd.none

let update (msg: Msg) (model: Model) : Model*Cmd<Msg> =
    match msg with
    | Error e ->
        printfn $"!! {e}"
        model, Cmd.none
    
    | AddDockedModel -> 
        let m, cmd = setupModelCmd()
        { model with DockedModels = model.DockedModels |> Array.append [|m|] }, cmd
    
    | ChartPageMsg (id, msg) ->
        match msg with
        | ChartPage.Msg.RemoveDockedModel ->
            printfn "Program: ChartPage.Msg.RemoveDockedModel"
            // let c = Cmd.OfFunc.attempt garbageCollect () Error
            let c = DisposeGraphData |> Cmd.ofMsg
            
            { model with
                GraphDataToDispose =
                     model.GraphDataToDispose
                     |> Array.append (
                             model.DockedModels
                             |> Array.tryFind (fun m -> m.Id = id)
                             |> Option.map (fun m -> m.GraphData)
                             |> Option.defaultValue [||]
                        )
                DockedModels = model.DockedModels |> Array.filter (fun x -> x.Id <> id)}, c
        
        | _ ->
            let updatedModel, paneCmd =
                        model.DockedModels
                        |> Array.tryFind (fun m -> m.Id = id)
                        |> Option.map (fun subModel -> 
                            let subModel', subCmd = ChartPage.update msg subModel
                            { model with 
                                DockedModels = 
                                    model.DockedModels 
                                    |> Array.map (fun m -> if m.Id = id then subModel' else m) },
                            Cmd.map (fun c -> ChartPageMsg(id, c)) subCmd)
                        |> Option.defaultValue (model, Cmd.none)
            updatedModel, Cmd.batch [ paneCmd;]
            
    | DisposeGraphData ->
        printfn "Program: Clearing and disposing graph data..."
        
        model.GraphDataToDispose 
        |> Array.iter
               (fun g ->
                    g.Clear()
                    if not (isNull g.ParentSurface) then
                        g.ParentSurface.Dispose())
        
        printfn "Program: Graph data cleared and disposed"
        { model with GraphDataToDispose = [||]}, Cmd.none
        
let bindings (): Binding<Model, Msg> list = [
    "DockedModels" |> Binding.subModelSeq ((fun (m: Model) -> m.DockedModels), snd, (fun (m: ChartPage.Model) -> m.Id), (ChartPageMsg), ChartPage.bindings)
    "AddDockedModel" |> Binding.cmd AddDockedModel
]

[<EntryPoint; STAThread>]
let main _ =
  Program.mkProgramWpf init update bindings
  |> Program.withConsoleTrace
  |> Program.runWindow (MainWindow())
