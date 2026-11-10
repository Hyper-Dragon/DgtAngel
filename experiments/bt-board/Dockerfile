# Debian rather than python:slim so that python3-dbus and python3-gi come from
# the distro and match the interpreter - a pip-installed dbus binding for a
# newer Python than the distro packages is a reliable way to lose an hour.
FROM debian:bookworm-slim

RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      python3 \
      python3-dbus \
      python3-gi \
 && rm -rf /var/lib/apt/lists/*

COPY board.py /app/board.py

# Unbuffered, so the log appears in `docker logs` the moment it happens.
ENV PYTHONUNBUFFERED=1

ENTRYPOINT ["python3", "-u", "/app/board.py"]
