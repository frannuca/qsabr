namespace qirvol.qtime

[<Measure>] type day

[<Measure>] type week

[<Measure>] type month

[<Measure>] type year

module timeconversions=
    let years2days = 360.0<day>/1.0<year>
    let years2months = 12.0<month>/1.0<year>

    let days2year = 1.0<year>/360.0<day>



