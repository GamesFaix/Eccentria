﻿module Config

open System.Net.Http

let client = new HttpClient()    
let cookie = "remember_web_59ba36addc2b2f9401580f014c7f58ea4e30989d=eyJpdiI6IkZkTVl5dE5ScEJ1Y0xWeUpNZktYckE9PSIsInZhbHVlIjoiXC9FbVdEZWJlRUlHNDhzTmVMZVVCSWxNaWFSbCtNcFV1N0E4UjI4cDF6YkQ1VmtGXC9STFpHYm1raWVFYTF4dERaWTlmQkxSdTc2bU03VDF3c3h1RHlPWDlLOGVCUHBkdFZnWHNMblwvNjFuQXc9IiwibWFjIjoiOTM5ZDhkMTQwY2RlMzQ4M2IyZTM1ZjM1Y2ZlYjI0YjI2NmVhNDg1ZWU5YjI4ZWE0ZmViYmIyZmU3NjkzNWU3YiJ9; XSRF-TOKEN=eyJpdiI6IkNjMzhEcmVsaVpQb3JKR0VDckxcL1Z3PT0iLCJ2YWx1ZSI6IjZGbEQ2cTZtb0NpbE9DWnJxXC9oMlwvT0hOREN3T0g4cHIrVkphWHRSazFxblRvTmFFUkJaM0VoREhhZFNpRkFjZzFEQ29nRVBVN0Q2YU5yd1E0K3Z1Tmc9PSIsIm1hYyI6ImM5ZTc1YTY3YWM3ODJjYmZiMWUzNTkzZGE3NWJiNzcyNjlkYzFjYTIxZWZkZmJmNzM3MWRiYTU1YjFlMjM3ZGQifQ%3D%3D; laravel_session=fbd5eec3495cd3f3de7464700746e7283043afc9"