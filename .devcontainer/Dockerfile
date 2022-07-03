# See here for image contents: https://github.com/microsoft/vscode-dev-containers/blob/main/containers/dotnet/.devcontainer/base.Dockerfile
# See here for latests container tags available: https://mcr.microsoft.com/v2/dotnet/sdk/tags/list


# [Choice] .NET version (container tag)
# Check with PowerShell: Invoke-Restmethod https://mcr.microsoft.com/v2/dotnet/sdk/tags/list| foreach-object {$_.tags -match "0-bullseye-slim-amd64$"} | select-object -last 2 # pick one
ARG VARIANT=6.0-bullseye-slim-amd64
FROM mcr.microsoft.com/dotnet/sdk:${VARIANT}

# Copy library scripts to execute
COPY library-scripts/*.sh library-scripts/*.env /tmp/library-scripts/

# [Option] Install zsh
ARG INSTALL_ZSH="false"
# [Option] Upgrade OS packages to their latest versions
ARG UPGRADE_PACKAGES="true"
# Install needed packages and setup non-root user. Use a separate RUN statement to add your own dependencies.
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID
RUN bash /tmp/library-scripts/common-debian.sh "${INSTALL_ZSH}" "${USERNAME}" "${USER_UID}" "${USER_GID}" "${UPGRADE_PACKAGES}" "true" "true" \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Install Azure CLI, PowerShell, create vscode user
# https://github.com/microsoft/vscode-dev-containers/tree/main/script-library
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

# Remove library scripts for final image
RUN rm -rf /tmp/library-scripts
