# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.177.0/containers/dotnet/.devcontainer/base.Dockerfile

# Dotnet SDK version: 3.1
ARG VARIANT="3.1"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:0-${VARIANT}

# Install Azure CLI
COPY library-scripts/*.sh library-scripts/*.env /tmp/library-scripts/
RUN bash /tmp/library-scripts/azcli-debian.sh \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/* /tmp/library-scripts

# Azure Functions core tools
RUN USERNAME=vscode \
    && USER_UID=1000 \
    && USER_GID=1000 \
    && apt-get update \
    && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends apt-utils dialog 2>&1 \ 
    && apt-get -y install git openssh-client less unzip iproute2 procps curl apt-transport-https gnupg2 lsb-release \ 
    && apt-get install -y sudo \ 
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL >/etc/sudoers.d/$USERNAME \ 
    && chmod 0440 /etc/sudoers.d/$USERNAME \ 
    && wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor >microsoft.asc.gpg \ 
    && mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ \ 
    && wget -q https://packages.microsoft.com/config/debian/9/prod.list \ 
    && mv prod.list /etc/apt/sources.list.d/microsoft-prod.list \ 
    && chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg \ 
    && chown root:root /etc/apt/sources.list.d/microsoft-prod.list \ 
    && echo "deb [arch=amd64] https://packages.microsoft.com/repos/azure-cli/ $(lsb_release -cs) main" >/etc/apt/sources.list.d/azure-cli.list \ 
    && apt-get update \ 
    && apt-get -y install azure-cli azure-functions-core-tools-3 \ 
    && apt-get autoremove -y \ 
    && apt-get clean -y \ 
    && rm -rf /var/lib/apt/lists/*

# Dotnet 5.0 SDK
RUN export DEBIAN_FRONTEND=noninteractive \
    && wget -O - https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg \
    && mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ \
    && wget https://packages.microsoft.com/config/debian/9/prod.list \
    && mv prod.list /etc/apt/sources.list.d/microsoft-prod.list \
    && chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg \
    && chown root:root /etc/apt/sources.list.d/microsoft-prod.list \
    && apt-get update \
    && apt-get install -y apt-transport-https \
    && apt-get update \
    && apt-get install -y dotnet-sdk-5.0

# GitHub Cli
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo gpg --dearmor -o /usr/share/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt update \
    && apt install gh