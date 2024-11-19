/* eslint-disable no-undef */
import * as React from "react";
import { useState } from "react";
import { Button, RadioGroup, Radio, makeStyles, Field, Textarea } from "@fluentui/react-components";

interface AgentProcessingProps {
  getText: () => Promise<string>;
}
const useStyles = makeStyles({
  textPromptAndInsertion: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
  },
  textAreaField: {
    marginLeft: "20px",
    marginTop: "30px",
    marginBottom: "20px",
    marginRight: "20px",
    maxWidth: "50%",
  },
});

const AgentProcessing: React.FC<AgentProcessingProps> = (props: AgentProcessingProps) => {
  const [selectedValue, setSelectedValue] = useState<string>("");
  const [apiResponse, setApiResponse] = useState<string>("");
  const [mailAddress, setMailAddress] = useState<string>("");

  // eslint-disable-next-line no-undef
  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSelectedValue(event.target.value);
  };

  const handleTextChange = async (event: React.ChangeEvent<HTMLTextAreaElement>) => {
    setMailAddress(event.target.value);
  };

  const createSocialPost = async () => {
    const inputText = await props.getText();
    try {
      const response = await fetch("https://localhost:7240/processDocument", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ Document: inputText, SocialNetwork: selectedValue, MailAddress: mailAddress }),
      });
      if (response.ok) {
        setApiResponse("Post published successfully");
      } else {
        setApiResponse("Error publishing post");
      }
    } catch (error) {
      setApiResponse(error.message);
    }
  };

  const styles = useStyles();

  return (
    <div className={styles.textPromptAndInsertion}>
      <p>Choose your network:</p>
      <RadioGroup value={selectedValue} onChange={handleChange}>
        <Radio value="SharePoint" label="SharePoint" />
        <Radio value="LinkedIn" label="LinkedIn" />
        <Radio value="Facebook" label="Facebook" />
      </RadioGroup>
      <Field className={styles.textAreaField} size="large" label="Enter the mail address:">
        <Textarea size="large" onChange={handleTextChange} />
      </Field>
      <Button onClick={createSocialPost}>Share it!</Button>
      <p>{apiResponse}</p>
    </div>
  );
};

export default AgentProcessing;
