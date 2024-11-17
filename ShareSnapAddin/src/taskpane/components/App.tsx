import * as React from "react";
import Header from "./Header";
import HeroList, { HeroListItem } from "./HeroList";
import AgentProcessing from "./AgentsProcessing";
import { makeStyles } from "@fluentui/react-components";
import { getSelectedText } from "../taskpane";

interface AppProps {
  title: string;
}

const useStyles = makeStyles({
  root: {
    minHeight: "100vh",
  },
});

const App: React.FC<AppProps> = (props: AppProps) => {
  const styles = useStyles();
  // The list items are static and won't change at runtime,
  // so this should be an ordinary const, not a part of state.
  const listItems: HeroListItem[] = [];

  return (
    <div className={styles.root}>
      <Header logo="assets/logo-filled.png" title={props.title} message="ShareSnap" />
      <HeroList message="Share your articles like a pro!" items={listItems} />
      <AgentProcessing getText={getSelectedText} />
    </div>
  );
};

export default App;
