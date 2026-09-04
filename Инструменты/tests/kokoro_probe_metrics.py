"""Read-only Windows sampler, including the venv launcher's real Python child."""

import ctypes
from ctypes import wintypes
import threading


class ProcessEntry(ctypes.Structure):
    _fields_ = [("size", wintypes.DWORD), ("usage", wintypes.DWORD), ("pid", wintypes.DWORD),
                ("heap", ctypes.c_size_t), ("module", wintypes.DWORD), ("threads", wintypes.DWORD),
                ("parent", wintypes.DWORD), ("priority", wintypes.LONG), ("flags", wintypes.DWORD),
                ("name", wintypes.WCHAR * 260)]


class Memory(ctypes.Structure):
    _fields_ = [("size", wintypes.DWORD), ("faults", wintypes.DWORD)] + [
        (name, ctypes.c_size_t) for name in
        ("peak_ws", "ws", "peak_pool", "pool", "peak_nonpaged", "nonpaged", "pagefile", "peak_pagefile")]


class WorkerSampler:
    def __init__(self, launcher):
        self.launcher = launcher
        self.samples = {}
        self.stop_event = threading.Event()
        self.thread = threading.Thread(target=self.run, daemon=True)
        self.thread.start()

    def run(self):
        kernel = ctypes.WinDLL("kernel32", use_last_error=True)
        kernel.CreateToolhelp32Snapshot.argtypes = [wintypes.DWORD, wintypes.DWORD]
        kernel.CreateToolhelp32Snapshot.restype = wintypes.HANDLE
        kernel.Process32FirstW.argtypes = kernel.Process32NextW.argtypes = [wintypes.HANDLE, ctypes.POINTER(ProcessEntry)]
        kernel.CloseHandle.argtypes = [wintypes.HANDLE]
        kernel.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
        kernel.OpenProcess.restype = wintypes.HANDLE
        get_memory = ctypes.WinDLL("psapi").GetProcessMemoryInfo
        get_memory.argtypes = [wintypes.HANDLE, ctypes.POINTER(Memory), wintypes.DWORD]
        while not self.stop_event.is_set():
            snapshot = kernel.CreateToolhelp32Snapshot(2, 0)
            if snapshot != ctypes.c_void_p(-1).value:
                try:
                    entry = ProcessEntry()
                    entry.size = ctypes.sizeof(entry)
                    more = kernel.Process32FirstW(snapshot, ctypes.byref(entry))
                    while more:
                        if entry.pid == self.launcher.pid or entry.parent == self.launcher.pid:
                            handle = kernel.OpenProcess(0x410, False, entry.pid)
                            if handle:
                                try:
                                    value = Memory()
                                    value.size = ctypes.sizeof(value)
                                    if get_memory(handle, ctypes.byref(value), value.size):
                                        previous = self.samples.get(entry.pid, {})
                                        self.samples[entry.pid] = {"pid": entry.pid, "parentPid": entry.parent,
                                            "name": entry.name, "peakThreads": max(entry.threads, previous.get("peakThreads", 0)),
                                            "peakWorkingSetBytes": max(value.peak_ws, previous.get("peakWorkingSetBytes", 0)),
                                            "peakPrivateBytes": max(value.peak_pagefile, previous.get("peakPrivateBytes", 0))}
                                finally:
                                    kernel.CloseHandle(handle)
                        more = kernel.Process32NextW(snapshot, ctypes.byref(entry))
                finally:
                    kernel.CloseHandle(snapshot)
            self.stop_event.wait(0.25)

    def stop(self):
        self.stop_event.set()
        self.thread.join(timeout=3)
        return list(self.samples.values())
