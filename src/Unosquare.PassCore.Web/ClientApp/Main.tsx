import * as React from 'react';

//import Grid from '@material-ui/core/Grid/Grid';
//import Typography from '@material-ui/core/Typography/Typography';
//import { LoadingIcon } from 'uno-material-ui';
import { useEffectWithLoading } from 'uno-react';
import { EntryPoint } from './Components/EntryPoint';
import { loadReCaptcha } from './Components/GoogleReCaptcha';
import { GlobalContextProvider } from './Provider/GlobalContextProvider';
import { SnackbarContextProvider } from './Provider/SnackbarContextProvider';
import { resolveAppSettings } from './Utils/AppSettings';

export const Main: React.FunctionComponent<any> = () => {
    // const [settings, isLoading] = useEffectWithLoading(resolveAppSettings, {}, []);
    const [settings, isLoading] = useEffectWithLoading(resolveAppSettings, {});

    React.useEffect(() => {
        if (settings && settings.reCaptcha) {
            if (settings.reCaptcha.siteKey !== '') {
                loadReCaptcha();
            }
        }
    }, [settings]);

    if (isLoading) {
        return <></>;
    }

    document.getElementById('title').innerHTML = settings.applicationTitle;

    return (
        <GlobalContextProvider settings={settings}>
            <SnackbarContextProvider>
                <EntryPoint />
            </SnackbarContextProvider>
        </GlobalContextProvider>
        // <Router>
        //     <Route exact path="/">
        //         <GlobalContextProvider settings={settings}>
        //             <SnackbarContextProvider>
        //                 <EntryPoint />
        //             </SnackbarContextProvider>
        //         </GlobalContextProvider>
        //     </Route>
        //     <Route path="/acm" component={AppAcm} />
        // </Router>
    );
};
