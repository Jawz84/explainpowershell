# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.177.0/containers/dotnet/.devcontainer/base.Dockerfile

# Dotnet SDK version
ARG DOTNET_VERSION="6.0"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:0-${DOTNET_VERSION}

# Install Azure CLI, PowerShell, create vscode user
# https://github.com/microsoft/vscode-dev-containers/tree/main/script-library
COPY library-scripts/*.sh library-scripts/*.env /tmp/library-scripts/
RUN bash /tmp/library-scripts/azcli-debian.sh \
    && bash /tmp/library-scripts/powershell-debian.sh \
    && bash /tmp/library-scripts/common-debian.sh "false" "vscode" "1000" "1000" "true" "false"\
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/* /tmp/library-scripts

# Download Azure Functions Core Tools (at this time instructions in the following url did not work, hence workaround https://github.com/Azure/azure-functions-core-tools#1-set-up-package-feed )
RUN mkdir /usr/bin/func \
    && curl -L https://github.com/Azure/azure-functions-core-tools/releases/download/4.0.3971/Azure.Functions.Cli.linux-x64.4.0.3971.zip -o /usr/bin/func/Azure.Functions.Cli.linux-x64.zip \
    && cd /usr/bin/func \
    && unzip Azure.Functions.Cli.linux-x64.zip \
    && rm Azure.Functions.Cli.linux-x64.zip \
    && chmod +x func \
    && chmod +x gozip
ENV PATH=/usr/bin/func:$PATH

# GitHub Cli
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo gpg --dearmor -o /usr/share/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt update \
    && apt install gh \
    && apt-get clean -y