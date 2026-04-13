def read_dotenv(path):
    env = {}
    for line in str(read_file(path)).splitlines():
        line = line.strip()
        if line and not line.startswith('#') and '=' in line:
            k, _, v = line.partition('=')
            env[k.strip()] = v.strip()
    return env
