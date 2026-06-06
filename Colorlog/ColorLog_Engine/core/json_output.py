import json
import sys
import time
import numpy as np

class NumpyEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, np.integer):
            return int(obj)
        if isinstance(obj, np.floating):
            return float(obj)
        if isinstance(obj, np.ndarray):
            return obj.tolist()
        return super().default(obj)

class JsonOutputThrottler:
    def __init__(self, interval_seconds):
        self.interval_seconds = interval_seconds
        self.last_output_time = 0.0

    def emit(self, payload):
        current_time = time.time()
        if current_time - self.last_output_time < self.interval_seconds:
            return None
        print(json.dumps(payload, cls=NumpyEncoder))
        sys.stdout.flush()
        self.last_output_time = current_time
        return payload
