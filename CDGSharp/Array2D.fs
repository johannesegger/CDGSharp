module internal Array2D

let indices1 (array: 'a[,]) =
    Seq.init (Array2D.length1 array) (fun i -> i + Array2D.base1 array)

let indices2 (array: 'a[,]) =
    Seq.init (Array2D.length2 array) (fun i -> i + Array2D.base2 array)

let slice i1 i2 l1 l2 (array: 'a[,]) =
    Array2D.initBased i1 i2 l1 l2 (fun i1 i2 ->
        array.[i1, i2]
    )

let toSequence (array: 'a[,]) =
    indices1 array
    |> Seq.map (fun i1 ->
        indices2 array
        |> Seq.map (fun i2 ->
            array.[i1,i2]
        )
    )

let toFlatSequence array =
    array
    |> toSequence
    |> Seq.collect id

let fold2 fn state =
    toSequence
    >> Seq.map (Seq.fold fn state)
    >> Seq.toArray
