from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL, get_unity_downloader_cli_cmd

class Template_PrePackJob():
    
    def __init__(self, template, editor, agent):
        self.job_id = template_job_id_prepack(template["id"], editor["name"])
        self.yml = self.get_job_definition(template, editor, agent).get_yml()


    def get_job_definition(self, template, editor, agent):

        # construct job
        job = YMLJob()
        job.set_name(f'Pre-Pack {template["name"]} {editor["name"]}')
        job.set_agent(agent)
        job.add_commands( [
                f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
                f'unity-downloader-cli {get_unity_downloader_cli_cmd(editor,"windows")} -c editor --wait --published-only',
                f'.Editor\\Unity.exe -projectPath {template["packagename"]} -batchmode -quit'])
        job.add_primed_artifacts_templates(template["packagename"])
        return job