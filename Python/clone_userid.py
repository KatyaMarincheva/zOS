# Clone a z/OS RACF userid from a model userid
# Requires: modelid.txt (output of LISTUSER MODELID TSO OMVS)
# Produces: commands.txt (REXX exec ready to run on z/OS)

import re

def build_adduser(new_userid, new_user_name, new_user_password, list_model_user, f):

    # Match USER= line and extract userid, owner, default group
    pattern_user = r"USER=([^\s]+)\s+.+\s+OWNER=([^\s]+)\s+.+\s+DEFAULT-GROUP=([^\s]+)"
    matches_user = re.findall(pattern_user, list_model_user)
    for m in matches_user:
        f.write(f"ADDUSER {new_userid} NAME('{new_user_name}') OWNER({m[1]}) DFLTGRP({m[2]}) PASSWORD('{new_user_password}')\n")


def build_tso(list_model_user, f):
    pattern_tso = r"(?<!NO\s)TSO INFORMATION\s+-+\s+ACCTNUM=\s+(\w+)\s+PROC=\s+(\w+)\s+SIZE=\s+(\w+)\s+MAXSIZE=\s+(\w+)\s+UNIT=\s+(\w+)\s+USERDATA=\s+(\w+)"

    matches_tso = re.findall(pattern_tso, list_model_user)
    
    for m in matches_tso:
        f.write(f"TSO(ACCTNUM({m[0]}) PROC({m[1]}) SIZE({m[2]}) MAXSIZE({m[3]}) UNIT({m[4]}) USERDATA({m[5]})\n")


def build_omvs(list_model_user, f):
    pattern_omvs = r"(?<!NO\s)OMVS INFORMATION\s+-+\s+.+\s+HOME=\s(\S+)\s+PROGRAM=\s(\S+)\s+CPUTIMEMAX=\s(\S+)\s+ASSIZEMAX=\s(\S+)\s+FILEPROCMAX=\s(\S+)\s+PROCUSERMAX=\s(\S+)\s+THREADSMAX=\s(\S+)\s+MMAPAREAMAX=\s([0-9]+)"

    matches_omvs = re.findall(pattern_omvs, list_model_user)

    for m in matches_omvs:
        f.write(f"OMVS(autouid HOME({m[0]}) PROGRAM({m[1]}) CPUTIMEMAX({m[2]}) ASSIZEMAX({m[3]}) FILEPROCMAX({m[4]}) PROCUSERMAX({m[5]}) THREADSMAX({m[6]}) MMAPAREAMAX({m[7]})\n")


def build_connect(new_userid, list_model_user, f):
    pattern_connect = r"(?<!DEFAULT-)GROUP=([A-Z]+)\s+AUTH=([A-Z]+)\s+CONNECT-OWNER=([A-Z]+)\s+.+\s+.+UACC=([A-Z]+)\s+.+\s+CONNECT ATTRIBUTES=([A-Z]+)"

    matches_connect = re.findall(pattern_connect, list_model_user)

    for m in matches_connect:
        f.write(f"CONNECT {new_userid} GROUP({m[0]}) AUTHORITY({m[1]}) OWNER({m[2]}) UACC({m[3]})\n")


def warn_attributes(list_model_user):
    pattern_attributes = r"ATTRIBUTES=((?!NONE).*)"

    matches_attributes = re.findall(pattern_attributes, list_model_user)

    if len(matches_attributes) > 0:
        print(f"Please note that the modelid has got the following privileged access ATTRIBUTES which have to be added manually: ")
 
        for m in matches_attributes:
            print(m)


if __name__ == "__main__":
    with open("modelid.txt", "r") as f:
        list_model_user = f.read()

    warn_attributes(list_model_user)
    
    new_userid = input("Please enter the new userid: ")

    new_user_name = input("Please enter the new user's name: ")

    new_user_password = input("Please enter the new user's password: ")

    with open("commands.txt", "w") as f:
        f.write("/*REXX*/\n")
        build_adduser(new_userid, new_user_name, new_user_password, list_model_user, f)
        build_tso(list_model_user, f)
        build_omvs(list_model_user, f)
        build_connect(new_userid, list_model_user, f)
        f.write("exit\n")
    
