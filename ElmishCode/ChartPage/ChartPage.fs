module ChartPage

open Elmish
open Elmish.WPF
open SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries
open Views
open System

type Model = {
    Id: Guid
    GraphData: UniformHeatmapDataSeries<double,double,int16> array
    DisplayIndex: int
    IsNextFrameLoaded: bool
}

type Msg =
    | Error of exn
    | ShowNextFrame
    | NewGraphLoaded of UniformHeatmapDataSeries<double, double, int16>
    | Close
    | RemoveDockedModel

// Async function to generate a new frame of randomized data
let generateNewGraphData dispatch =
    let buildGraph onDone =
        async {
            let randomGenerator = Random()
            UniformHeatmapDataSeries<double,double,int16>(Array2D.init 10000 10000 (fun i j -> randomGenerator.Next(101) |> int16), 0, 1, 0, 1) |> onDone
        }
    async {
        let onDone = 
            fun newGraph -> newGraph |> NewGraphLoaded |> dispatch
        do! buildGraph onDone
    } |> Async.Start

let garbageCollect () =
    GC.Collect()

let init(id: Guid): Model*Cmd<Msg> = 
    let randomGenerator = Random()
    {
        Id = id
        GraphData = [| 
            UniformHeatmapDataSeries<double,double,int16>(Array2D.init 10000 10000 (fun i j -> randomGenerator.Next(101) |> int16), 0,1,0,1)
            UniformHeatmapDataSeries<double,double,int16>(Array2D.init 10000 10000 (fun i j -> randomGenerator.Next(101) |> int16), 0,1,0,1)
        |]
        DisplayIndex = 0
        IsNextFrameLoaded = true
    }, Cmd.none

let update (msg: Msg) (model: Model) : Model*Cmd<Msg> =
    match msg with
    | Error e -> printfn "!! %O" e; model, Cmd.none
    
    // Display the next frame in the graph data
    | ShowNextFrame ->
        model.GraphData[model.DisplayIndex].Clear()
        { model with DisplayIndex = (model.DisplayIndex + 1) % model.GraphData.Length 
                     IsNextFrameLoaded = false },
        generateNewGraphData |> Cmd.ofSub

    // Callback when new graph data is generated
    | NewGraphLoaded newGraph ->
        { model with GraphData = model.GraphData |> Array.mapi (fun i x -> if i = (model.DisplayIndex + 1) % model.GraphData.Length then newGraph else x)
                     IsNextFrameLoaded = true },
        Cmd.OfFunc.attempt garbageCollect () Error

    | Close ->
        // model.GraphData 
        // |> Array.iter (fun g -> 
        //                 g.Clear()
        //                 if (not(isNull(g.ParentSurface))) then
        //                     g.ParentSurface.Dispose())
        // { model with GraphData = [||]}, RemoveDockedModel |> Cmd.ofMsg
        model, RemoveDockedModel |> Cmd.ofMsg

    | RemoveDockedModel ->
        model, Cmd.none

let bindings (): Binding<Model, Msg> list = [
    "GraphData" |> Binding.oneWayOpt (fun m -> m.GraphData |> Array.tryItem m.DisplayIndex)
    "ShowNextFrame" |> Binding.cmd ShowNextFrame
    "IsNextFrameLoaded" |> Binding.oneWay (fun m -> m.IsNextFrameLoaded)
    "CurrentFrame" |> Binding.oneWay (fun m -> m.DisplayIndex)
    "ClosePage" |> Binding.cmd Close
    ]