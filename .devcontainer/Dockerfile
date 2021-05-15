# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.177.0/containers/dotnet/.devcontainer/base.Dockerfile

# [Choice] .NET version: 5.0, 3.1, 2.1
ARG VARIANT="3.1"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:0-${VARIANT}

# [Option] Install Azure CLI
ARG INSTALL_AZURE_CLI="true"
COPY library-scripts/*.sh library-scripts/*.env /tmp/library-scripts/
RUN if [ "$INSTALL_AZURE_CLI" = "true" ]; then bash /tmp/library-scripts/azcli-debian.sh; fi \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/* /tmp/library-scripts

RUN USERNAME=vscode \
    && USER_UID=1000 \
    && USER_GID=1000 \
    && apt-get update \
    && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends apt-utils dialog 2>&1 \ 
    && apt-get -y install git openssh-client less unzip iproute2 procps curl apt-transport-https gnupg2 lsb-release \ 
    && groupadd --gid $USER_GID $USERNAME \ 
    && useradd -s /bin/bash --uid $USER_UID --gid $USER_GID -m $USERNAME \ 
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

# RUN && export DEBIAN_FRONTEND=noninteractive \
#     && dotnet_sdk_version=3.1.408 \
#     && curl -SL --output dotnet.tar.gz https://dotnetcli.azureedge.net/dotnet/Sdk/$dotnet_sdk_version/dotnet-sdk-$dotnet_sdk_version-linux-x64.tar.gz \
#     && dotnet_sha512='5e2c378addf337c1b7e44583718672e0791235150630f0b613353c8597342dcaae3b7c412b17d5e6d93d87b5708a6ca2dc16ff6e6c5a2101b224d24e6a5c4e7a' \
#     && echo "$dotnet_sha512 dotnet.tar.gz" | sha512sum -c - \
#     && mkdir -p /usr/share/dotnet \
#     && tar -ozxf dotnet.tar.gz -C /usr/share/dotnet \
#     && rm dotnet.tar.gz

RUN export DEBIAN_FRONTEND=noninteractive \
    && wget https://packages.microsoft.com/config/debian/9/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y apt-transport-https \
    && apt-get update \
    && apt-get install -y dotnet-sdk-5.0