# Load Tests

Prerequisites: Install k6 from https://k6.io/docs/get-started/installation/

## Run

```bash
k6 run loadtests/stations-load.js
k6 run loadtests/dashboard-load.js
k6 run --env BASE_URL=http://prod:5000 loadtests/stations-load.js
```
