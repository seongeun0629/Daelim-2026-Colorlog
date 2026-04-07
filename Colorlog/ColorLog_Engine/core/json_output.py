import json
import sys
import time


class JsonOutputThrottler:
    def __init__(self, interval_seconds):
        self.interval_seconds = interval_seconds
        self.last_output_time = 0.0

    def emit(self, payload):
        current_time = time.time()
        if current_time - self.last_output_time < self.interval_seconds:
            return

        print(json.dumps(payload))
        sys.stdout.flush()
        self.last_output_time = current_time

