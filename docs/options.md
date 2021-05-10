# Options

Options allow you to specify how a test run is executed.

| Option     | Description                                                  |
| ---------- | ------------------------------------------------------------ |
| VUs        | The VUs to run concurrently                                  |
| Iterations | Fixed number of iterations to execute the load test. The number of iterations is split between all VUs defined. <br />*An alternative is to define the duration instead.* |
| Duration   | Total duration of the load test. During this time each VU will be executed in a loop. |

