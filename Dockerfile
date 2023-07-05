# Start from the latest Ubuntu image
FROM ubuntu:23.04

# Update Ubuntu Software repository
RUN apt-get update

# Install PostgreSQL
RUN apt-get install -y postgresql postgresql-contrib

# Set the locale
RUN apt-get install -y locales
RUN locale-gen en_US.UTF-8
RUN update-locale LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8

# Set environment variables for PostgreSQL
USER postgres
RUN    /etc/init.d/postgresql start &&\
    psql --command "CREATE USER supersoanxuser WITH SUPERUSER PASSWORD 'PVWjT2R9nYw4LzY9ELpc';" &&\
    createdb -O soanx -E UTF8 soanx

# Expose the PostgreSQL port
EXPOSE 5432

# Switch back to the root user
USER root

# Run .NET app
CMD /bin/bash -c "/etc/init.d/postgresql start"
