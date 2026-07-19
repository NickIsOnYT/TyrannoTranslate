import sys
import io
import json
import os
import subprocess
import importlib.util

sys.stdin = io.TextIOWrapper(sys.stdin.buffer, encoding="utf-8-sig")
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")


def ensure_package(package_name):
    spec = importlib.util.find_spec(package_name)
    if spec is None:
        subprocess.check_call(
            [sys.executable, "-m", "pip", "install", package_name],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )


def main():
    try:
        input_raw = sys.stdin.read()
        input_data = json.loads(input_raw) if input_raw else {}
        packages_dir = input_data.get("packages_dir", "")

        if packages_dir:
            os.environ["ARGOS_PACKAGES_DIR"] = packages_dir

        ensure_package("argostranslate")

        import argostranslate.package

        argostranslate.package.update_package_index()

        installed = argostranslate.package.get_installed_packages()
        installed_map = {}
        for p in installed:
            key = f"{p.from_code}->{p.to_code}"
            installed_map[key] = True

        available = argostranslate.package.get_available_packages()
        available_set = set()
        for p in available:
            available_set.add(p.from_code)
            available_set.add(p.to_code)

        output = {
            "success": True,
            "installed": installed_map,
            "available_languages": sorted(available_set),
        }
        sys.stdout.write(json.dumps(output, ensure_ascii=False))
        sys.stdout.flush()

    except Exception as e:
        error_msg = f"{type(e).__name__}: {e}"
        output = {"success": False, "error": error_msg}
        sys.stdout.write(json.dumps(output, ensure_ascii=False))
        sys.stdout.flush()


if __name__ == "__main__":
    main()
